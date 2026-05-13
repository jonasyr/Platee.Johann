using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Infrastructure.Json;

namespace Platee.Johann.Tests.Unit;

public sealed class JsonSettingsRepositoryTests : IDisposable
{
    private readonly string _tempDir;
    private readonly JsonSettingsRepository _sut;

    public JsonSettingsRepositoryTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"JohannSettingsTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _sut = new JsonSettingsRepository(_tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    // ── Missing file → defaults ───────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_WhenNoFileExists_ReturnsDefaults()
    {
        var settings = await _sut.LoadAsync();

        settings.PromptDefaultsRevision.Should().Be(PromptDefaultsMigration.CurrentRevision);
        settings.StructuredPrompt.Should().Be(SummaryPrompts.Structured);
        settings.EmailPrompt.Should().Be(SummaryPrompts.Email);
        settings.AufgabePrompt.Should().Be(SummaryPrompts.Aufgabe);
    }

    // ── Round-trip ────────────────────────────────────────────────────────────

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesAllPrompts()
    {
        var original = AppSettings.Default with
        {
            Name = "Test User",
            Firma = "Test GmbH",
        };

        await _sut.SaveAsync(original);
        var loaded = await _sut.LoadAsync();

        loaded.Name.Should().Be("Test User");
        loaded.PromptDefaultsRevision.Should().Be(PromptDefaultsMigration.CurrentRevision);
        loaded.StructuredPrompt.Should().Be(SummaryPrompts.Structured);
        loaded.EmailPrompt.Should().Be(SummaryPrompts.Email);
        loaded.AufgabePrompt.Should().Be(SummaryPrompts.Aufgabe);
    }

    // ── Foreign paths are preserved by repository (fallback is App's job) ────

    [Fact]
    public async Task LoadAsync_WhenSettingsHasForeignPaths_ReturnsThosePaths()
    {
        // Repository loads what's in the file; path fallback is handled in App.xaml.cs.
        var foreignSettings = AppSettings.Default with
        {
            Quellverzeichnis = @"D:\OneDrive\SomeForeignMachine\Input",
            Ausgabeverzeichnis = @"D:\OneDrive\SomeForeignMachine\Output",
            Archivverzeichnis = @"D:\OneDrive\SomeForeignMachine\Archiv",
        };

        await _sut.SaveAsync(foreignSettings);
        var loaded = await _sut.LoadAsync();

        loaded.Quellverzeichnis.Should().Be(@"D:\OneDrive\SomeForeignMachine\Input");
        loaded.Ausgabeverzeichnis.Should().Be(@"D:\OneDrive\SomeForeignMachine\Output");
    }

    // ── Corrupt file → defaults ───────────────────────────────────────────────

    [Fact]
    public async Task LoadAsync_WhenFileIsCorrupt_ReturnsDefaults()
    {
        var settingsPath = Path.Combine(_tempDir, "settings.json");
        await File.WriteAllTextAsync(settingsPath, "{ this is not valid json }}}");

        var settings = await _sut.LoadAsync();

        settings.PromptDefaultsRevision.Should().Be(PromptDefaultsMigration.CurrentRevision);
        settings.StructuredPrompt.Should().Be(SummaryPrompts.Structured);
        settings.Name.Should().Be(AppSettings.Default.Name);
    }

    // ── Partial file → missing fields fall back to defaults ──────────────────

    [Fact]
    public async Task LoadAsync_WhenFileHasOnlyName_FillsPromptsFromDefaults()
    {
        var settingsPath = Path.Combine(_tempDir, "settings.json");
        await File.WriteAllTextAsync(settingsPath, """{"name": "Partial User"}""");

        var settings = await _sut.LoadAsync();

        settings.Name.Should().Be("Partial User");
        settings.PromptDefaultsRevision.Should().Be(0);
        settings.StructuredPrompt.Should().Be(SummaryPrompts.Structured);
        settings.EmailPrompt.Should().Be(SummaryPrompts.Email);
    }

    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesPromptDefaultsRevision()
    {
        var original = AppSettings.Default with
        {
            PromptDefaultsRevision = 123,
        };

        await _sut.SaveAsync(original);
        var loaded = await _sut.LoadAsync();

        loaded.PromptDefaultsRevision.Should().Be(123);
    }
}
