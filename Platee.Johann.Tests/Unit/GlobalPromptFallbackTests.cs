using FluentAssertions;
using Platee.Johann.Application.Services;
using Platee.Johann.Application.Settings;
using Platee.Johann.Infrastructure.Json;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Covers audit finding H1 end-to-end with real repositories: when the team share
/// is unreachable the app used to fall back to built-in defaults without a word,
/// so one colleague silently produced different GPT output than everyone else.
/// The local cache of the last successful load is what makes that survivable.
/// </summary>
public sealed class GlobalPromptFallbackTests : IDisposable
{
    private readonly string localDir = Path.Combine(
        Path.GetTempPath(), "johann-local-" + Guid.NewGuid().ToString("N"));

    private readonly string shareDir = Path.Combine(
        Path.GetTempPath(), "johann-share-" + Guid.NewGuid().ToString("N"));

    private readonly JsonPromptSettingsRepository cache;
    private readonly string globalPath;

    public GlobalPromptFallbackTests()
    {
        Directory.CreateDirectory(this.localDir);
        Directory.CreateDirectory(this.shareDir);
        this.cache = JsonPromptSettingsRepository.FromFilePath(
            Path.Combine(this.localDir, "prompts.cache.json"));
        this.globalPath = Path.Combine(this.shareDir, "prompts.json");
    }

    public void Dispose()
    {
        foreach (var dir in new[] { this.localDir, this.shareDir })
        {
            try
            {
                Directory.Delete(dir, recursive: true);
            }
            catch (IOException)
            {
                // Temp cleanup only.
            }
        }
    }

    private async Task<PromptStartupResult> StartupAsync() =>
        await PromptStartupResolver.ResolveAsync(
            this.cache,
            JsonPromptSettingsRepository.FromFilePath(this.globalPath),
            this.globalPath);

    [Fact]
    public async Task WhenShareIsGone_TheLastKnownTeamPromptsAreUsed_NotBuiltInDefaults()
    {
        await WriteGlobalAsync("Team-Systemnachricht");
        (await this.StartupAsync()).Prompts.SystemMessage.Should().Be("Team-Systemnachricht");

        File.Delete(this.globalPath);

        var afterOutage = await this.StartupAsync();

        afterOutage.Prompts.SystemMessage.Should().Be("Team-Systemnachricht");
        afterOutage.Prompts.SystemMessage.Should().NotBe(PromptSettings.Default.SystemMessage);
    }

    [Fact]
    public async Task WhenShareIsGone_TheFallbackIsReported()
    {
        await WriteGlobalAsync("Team-Systemnachricht");
        (await this.StartupAsync()).Warning.Should().BeNull();
        File.Delete(this.globalPath);

        var afterOutage = await this.StartupAsync();

        afterOutage.Warning.Should().NotBeNullOrWhiteSpace();
        afterOutage.Warning.Should().Contain(this.globalPath);
        afterOutage.Warning.Should().Contain("Zwischenspeicher");
    }

    [Fact]
    public async Task WhenShareIsCorrupt_TheCacheIsNotOverwrittenWithDefaults()
    {
        await WriteGlobalAsync("Team-Systemnachricht");
        await this.StartupAsync();

        File.WriteAllText(this.globalPath, "{ kaputt");

        var afterCorruption = await this.StartupAsync();

        afterCorruption.Prompts.SystemMessage.Should().Be("Team-Systemnachricht");
        afterCorruption.Warning.Should().NotBeNullOrWhiteSpace();

        // And the cache on disk must still hold the team prompt, not defaults.
        var reread = await JsonPromptSettingsRepository
            .FromFilePath(Path.Combine(this.localDir, "prompts.cache.json")).LoadAsync();
        reread.SystemMessage.Should().Be("Team-Systemnachricht");
    }

    [Fact]
    public async Task WhenShareReturns_ItsPromptsWinAgain()
    {
        await WriteGlobalAsync("Alte Team-Nachricht");
        await this.StartupAsync();
        File.Delete(this.globalPath);
        await this.StartupAsync();

        await WriteGlobalAsync("Neue Team-Nachricht");
        var afterRecovery = await this.StartupAsync();

        afterRecovery.Prompts.SystemMessage.Should().Be("Neue Team-Nachricht");
        afterRecovery.Warning.Should().BeNull();
    }

    [Fact]
    public async Task WhenNoShareWasEverConfigured_DefaultsAreUsedWithoutAFallbackReport()
    {
        var result = await PromptStartupResolver.ResolveAsync(this.cache, globalRepo: null, globalPath: null);

        result.Warning.Should().BeNull();
        result.Prompts.SystemMessage.Should().Be(PromptSettings.Default.SystemMessage);
    }

    [Fact]
    public async Task WhenShareIsGoneAndNoCacheExists_TheWarningSaysResultsWillDiffer()
    {
        var afterOutage = await this.StartupAsync();

        afterOutage.Prompts.SystemMessage.Should().Be(PromptSettings.Default.SystemMessage);
        afterOutage.Warning.Should().Contain("weichen von denen der Kollegen ab");
    }

    private async Task WriteGlobalAsync(string systemMessage) =>
        await JsonPromptSettingsRepository.FromFilePath(this.globalPath)
            .SaveAsync(PromptSettings.Default with { SystemMessage = systemMessage });
}
