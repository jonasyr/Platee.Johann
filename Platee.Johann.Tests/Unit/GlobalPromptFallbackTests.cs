using FluentAssertions;
using Platee.Johann.Application.Interfaces;
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
    private readonly string cachePath;

    public GlobalPromptFallbackTests()
    {
        Directory.CreateDirectory(this.localDir);
        Directory.CreateDirectory(this.shareDir);
        // Per-share name, deliberately distinct from the legacy flat "prompts.cache.json".
        this.cachePath = Path.Combine(this.localDir, "prompts.cache.abcd1234.json");
        this.cache = JsonPromptSettingsRepository.FromFilePath(this.cachePath);
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
        var reread = await JsonPromptSettingsRepository.FromFilePath(this.cachePath).LoadAsync();
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

    // ── PR #46 review findings ────────────────────────────────────────────────
    [Fact]
    public async Task WhenTheGlobalPathIsCleared_TheCacheIsIgnored()
    {
        // Clearing the path is the user switching team prompts off. Serving them
        // the cached team prompts anyway — silently — is the same class of bug the
        // whole audit was about.
        await WriteGlobalAsync("Team-Systemnachricht");
        await this.StartupAsync();

        var withoutShare = await PromptStartupResolver.ResolveAsync(
            this.cache, globalRepo: null, globalPath: null);

        withoutShare.Prompts.SystemMessage.Should().Be(PromptSettings.Default.SystemMessage);
        withoutShare.Warning.Should().BeNull();
    }

    [Fact]
    public async Task WhenTheShareVanishesDuringTheRead_TheCacheSurvives()
    {
        await WriteGlobalAsync("Team-Systemnachricht");
        await this.StartupAsync();

        // IsReachable answers from a file that is gone by the time it is read.
        var vanishing = new VanishingRepository(this.globalPath);
        var result = await PromptStartupResolver.ResolveAsync(
            this.cache, vanishing, this.globalPath);

        result.Prompts.SystemMessage.Should().Be("Team-Systemnachricht");
        result.Warning.Should().NotBeNullOrWhiteSpace();

        var reread = await JsonPromptSettingsRepository.FromFilePath(this.cachePath).LoadAsync();
        reread.SystemMessage.Should().Be("Team-Systemnachricht");
    }

    [Fact]
    public async Task WhenTheCacheIsCorrupt_TheWarningDoesNotClaimTeamPromptsAreInUse()
    {
        await WriteGlobalAsync("Team-Systemnachricht");
        await this.StartupAsync();

        File.WriteAllText(this.cachePath, "{ kaputt");
        File.Delete(this.globalPath);

        var result = await this.StartupAsync();

        result.Prompts.SystemMessage.Should().Be(PromptSettings.Default.SystemMessage);
        result.Warning.Should().Contain("weichen von denen der Kollegen ab");
        result.Warning.Should().NotContain("Zwischenspeicher");
    }

    [Fact]
    public void CacheFileNameFor_DistinguishesShares_AndIsStable()
    {
        var a = PromptStartupResolver.CacheFileNameFor(@"Z:\12_Tools\Peano\Johann\prompts.json");
        var b = PromptStartupResolver.CacheFileNameFor(@"Y:\anderes\Team\prompts.json");

        a.Should().NotBe(b);
        a.Should().Be(PromptStartupResolver.CacheFileNameFor(@"z:/12_Tools/Peano/Johann/prompts.json"));
        a.Should().StartWith("prompts.cache.").And.EndWith(".json");
    }

    [Fact]
    public async Task UpgradingWhileTheShareIsDown_StillUsesTheOldFlatCache()
    {
        // Reproduces the upgrade window: the flat cache from the previous version
        // is all we have, no per-share cache exists yet, and the share is down.
        var legacy = Path.Combine(this.localDir, "prompts.cache.json");
        await JsonPromptSettingsRepository.FromFilePath(legacy)
            .SaveAsync(PromptSettings.Default with { SystemMessage = "Team-Systemnachricht" });
        File.Exists(this.cachePath).Should().BeFalse();

        PromptStartupResolver.AdoptLegacyCache(legacy, this.cachePath).Should().BeNull();
        var result = await this.StartupAsync();

        result.Prompts.SystemMessage.Should().Be("Team-Systemnachricht");
        File.Exists(legacy).Should().BeFalse("it was carried over, not copied");
    }

    [Fact]
    public async Task AdoptLegacyCache_WhenAPerShareCacheAlreadyExists_DropsTheFlatOne()
    {
        var legacy = Path.Combine(this.localDir, "prompts.cache.json");
        await JsonPromptSettingsRepository.FromFilePath(legacy)
            .SaveAsync(PromptSettings.Default with { SystemMessage = "alt" });
        await this.cache.SaveAsync(PromptSettings.Default with { SystemMessage = "aktuell" });

        PromptStartupResolver.AdoptLegacyCache(legacy, this.cachePath).Should().BeNull();

        File.Exists(legacy).Should().BeFalse();
        (await this.cache.LoadAsync()).SystemMessage.Should().Be("aktuell");
    }

    [Fact]
    public void AdoptLegacyCache_WhenThereIsNothingToAdopt_IsAQuietNoOp()
    {
        PromptStartupResolver
            .AdoptLegacyCache(Path.Combine(this.localDir, "prompts.cache.json"), this.cachePath)
            .Should().BeNull();

        File.Exists(this.cachePath).Should().BeFalse();
    }

    /// <summary>Reports the file as present, then finds it gone when reading.</summary>
    private sealed class VanishingRepository(string path) : IPromptSettingsRepository
    {
        public bool IsReachable => true;

        public SettingsFileFault? LastLoadFault => null;

        public bool LastLoadReadFile => false;

        public Task<PromptSettings> LoadAsync(CancellationToken ct = default)
        {
            _ = path;
            return Task.FromResult(PromptSettings.Default);
        }

        public Task SaveAsync(PromptSettings settings, CancellationToken ct = default) =>
            Task.CompletedTask;
    }

    private async Task WriteGlobalAsync(string systemMessage) =>
        await JsonPromptSettingsRepository.FromFilePath(this.globalPath)
            .SaveAsync(PromptSettings.Default with { SystemMessage = systemMessage });
}
