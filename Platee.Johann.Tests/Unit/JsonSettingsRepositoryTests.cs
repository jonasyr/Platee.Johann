namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Settings;
using Platee.Johann.Infrastructure.Json;

public sealed class JsonSettingsRepositoryTests : IDisposable
{
    private readonly string tempDir;
    private readonly JsonSettingsRepository sut;

    public JsonSettingsRepositoryTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), $"JohannSettingsTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.tempDir);
        this.sut = new JsonSettingsRepository(this.tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(this.tempDir))
        {
            Directory.Delete(this.tempDir, recursive: true);
        }
    }

    // ── Missing file → defaults ───────────────────────────────────────────────
    [Fact]
    public async Task LoadAsync_WhenNoFileExists_ReturnsDefaults()
    {
        var settings = await this.sut.LoadAsync();

        settings.Name.Should().Be(AppSettings.Default.Name);
        settings.Firma.Should().Be(AppSettings.Default.Firma);
    }

    // ── Round-trip ────────────────────────────────────────────────────────────
    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesUserSettings()
    {
        var original = AppSettings.Default with
        {
            Name = "Test User",
            Firma = "Test GmbH",
        };

        await this.sut.SaveAsync(original);
        var loaded = await this.sut.LoadAsync();

        loaded.Name.Should().Be("Test User");
        loaded.Firma.Should().Be("Test GmbH");
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

        await this.sut.SaveAsync(foreignSettings);
        var loaded = await this.sut.LoadAsync();

        loaded.Quellverzeichnis.Should().Be(@"D:\OneDrive\SomeForeignMachine\Input");
        loaded.Ausgabeverzeichnis.Should().Be(@"D:\OneDrive\SomeForeignMachine\Output");
    }

    // ── Corrupt file → defaults ───────────────────────────────────────────────
    [Fact]
    public async Task LoadAsync_WhenFileIsCorrupt_ReturnsDefaults()
    {
        var settingsPath = Path.Combine(this.tempDir, "settings.json");
        await File.WriteAllTextAsync(settingsPath, "{ this is not valid json }}}");

        var settings = await this.sut.LoadAsync();

        settings.Name.Should().Be(AppSettings.Default.Name);
    }

    // ── Partial file → missing fields fall back to defaults ──────────────────
    [Fact]
    public async Task LoadAsync_WhenFileHasOnlyName_FillsFromDefaults()
    {
        var settingsPath = Path.Combine(this.tempDir, "settings.json");
        await File.WriteAllTextAsync(settingsPath, """{"name": "Partial User"}""");

        var settings = await this.sut.LoadAsync();

        settings.Name.Should().Be("Partial User");
        settings.Firma.Should().Be(AppSettings.Default.Firma);
    }

    // ── GlobalPromptFilePath round-trip ──────────────────────────────────────
    [Fact]
    public async Task SaveAndLoad_RoundTrip_PreservesGlobalPromptFilePath()
    {
        var settings = AppSettings.Default with { GlobalPromptFilePath = @"Z:\prompts.json" };
        await this.sut.SaveAsync(settings);
        var loaded = await this.sut.LoadAsync();

        loaded.GlobalPromptFilePath.Should().Be(@"Z:\prompts.json");
    }
}
