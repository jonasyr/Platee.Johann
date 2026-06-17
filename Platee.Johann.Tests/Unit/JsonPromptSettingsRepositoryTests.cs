namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Infrastructure.Json;

public class JsonPromptSettingsRepositoryTests : IDisposable
{
    private readonly string tempDir;
    private readonly JsonPromptSettingsRepository sut;

    public JsonPromptSettingsRepositoryTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), "johann-test-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(this.tempDir);
        this.sut = new JsonPromptSettingsRepository(this.tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(this.tempDir))
        {
            Directory.Delete(this.tempDir, recursive: true);
        }
    }

    [Fact]
    public async Task LoadAsync_WhenNoFileExists_ReturnsDefaults()
    {
        var result = await this.sut.LoadAsync();

        result.SystemMessage.Should().Be(SummaryPrompts.SystemMessage);
        result.AbstractPrompt.Should().Be(SummaryPrompts.Abstract);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesAllPrompts()
    {
        var custom = PromptSettings.Default with
        {
            SystemMessage = "custom-system",
            AbstractPrompt = "custom-abstract",
            EmailPrompt = "custom-email",
        };

        await this.sut.SaveAsync(custom);
        var loaded = await this.sut.LoadAsync();

        loaded.SystemMessage.Should().Be("custom-system");
        loaded.AbstractPrompt.Should().Be("custom-abstract");
        loaded.EmailPrompt.Should().Be("custom-email");
        loaded.StructuredPrompt.Should().Be(SummaryPrompts.Structured);
    }

    [Fact]
    public async Task LoadAsync_WhenFileIsCorrupt_ReturnsDefaults()
    {
        var filePath = Path.Combine(this.tempDir, "prompts.json");
        await File.WriteAllTextAsync(filePath, "NOT VALID JSON {{{");

        var result = await this.sut.LoadAsync();

        result.SystemMessage.Should().Be(SummaryPrompts.SystemMessage);
    }

    [Fact]
    public async Task IsReachable_WhenFileExists_ReturnsTrue()
    {
        await this.sut.SaveAsync(PromptSettings.Default);
        this.sut.IsReachable.Should().BeTrue();
    }

    [Fact]
    public void IsReachable_WhenFileDoesNotExist_ReturnsFalse()
    {
        this.sut.IsReachable.Should().BeFalse();
    }

    [Fact]
    public async Task FromFilePath_LoadsFromSpecifiedFile()
    {
        var customFile = Path.Combine(this.tempDir, "custom-prompts.json");
        var repo = JsonPromptSettingsRepository.FromFilePath(customFile);
        var custom = PromptSettings.Default with { SystemMessage = "from-custom-file" };
        await repo.SaveAsync(custom);

        var loaded = await repo.LoadAsync();
        loaded.SystemMessage.Should().Be("from-custom-file");
        repo.IsReachable.Should().BeTrue();
    }
}
