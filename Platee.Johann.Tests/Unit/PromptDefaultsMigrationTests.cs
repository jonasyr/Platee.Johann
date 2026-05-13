using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;

namespace Platee.Johann.Tests.Unit;

public sealed class PromptDefaultsMigrationTests : IDisposable
{
    private readonly string _tempDir;
    private readonly string _settingsFilePath;

    public PromptDefaultsMigrationTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"JohannPromptMigrationTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);
        _settingsFilePath = Path.Combine(_tempDir, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, recursive: true);
    }

    [Fact]
    public void ApplyIfNeeded_WhenRevisionIsCurrent_DoesNothing()
    {
        var settings = AppSettings.Default;

        var result = PromptDefaultsMigration.ApplyIfNeeded(settings, _settingsFilePath);

        result.DidMigrate.Should().BeFalse();
        result.BackupPath.Should().BeNull();
        result.Settings.Should().BeEquivalentTo(settings);
    }

    [Fact]
    public void ApplyIfNeeded_WhenOldRevisionAndFileExists_BacksUpAndOverwritesOnlyPrompts()
    {
        File.WriteAllText(_settingsFilePath, """{"name":"Alt","systemMessage":"custom"}""");

        var settings = AppSettings.Default with
        {
            PromptDefaultsRevision = 0,
            Name = "Eigener Name",
            Firma = "Eigene Firma",
            Quellverzeichnis = @"D:\Input",
            Archivverzeichnis = @"D:\Archiv",
            Ausgabeverzeichnis = @"D:\Output",
            SystemMessage = "CUSTOM SYSTEM",
            AbstractPrompt = "CUSTOM ABSTRACT",
            StructuredPrompt = "CUSTOM STRUCTURED",
            ProsePrompt = "CUSTOM PROSE",
            EmailPrompt = "CUSTOM EMAIL",
            AufgabePrompt = "CUSTOM AUFGABE",
            GespraechsnotizPrompt = "CUSTOM GESPRAECH",
            StundenzettelPrompt = "CUSTOM STUNDEN",
            AnalogPrompt = "CUSTOM ANALOG",
        };

        var result = PromptDefaultsMigration.ApplyIfNeeded(settings, _settingsFilePath);

        result.DidMigrate.Should().BeTrue();
        result.BackupPath.Should().NotBeNull();
        File.Exists(result.BackupPath!).Should().BeTrue();
        File.ReadAllText(result.BackupPath!).Should().Contain("\"systemMessage\":\"custom\"");

        result.Settings.PromptDefaultsRevision.Should().Be(PromptDefaultsMigration.CurrentRevision);
        result.Settings.Name.Should().Be("Eigener Name");
        result.Settings.Firma.Should().Be("Eigene Firma");
        result.Settings.Quellverzeichnis.Should().Be(@"D:\Input");
        result.Settings.Archivverzeichnis.Should().Be(@"D:\Archiv");
        result.Settings.Ausgabeverzeichnis.Should().Be(@"D:\Output");
        result.Settings.SystemMessage.Should().Be(SummaryPrompts.SystemMessage);
        result.Settings.AbstractPrompt.Should().Be(SummaryPrompts.Abstract);
        result.Settings.StructuredPrompt.Should().Be(SummaryPrompts.Structured);
        result.Settings.ProsePrompt.Should().Be(SummaryPrompts.Prose);
        result.Settings.EmailPrompt.Should().Be(SummaryPrompts.Email);
        result.Settings.AufgabePrompt.Should().Be(SummaryPrompts.Aufgabe);
        result.Settings.GespraechsnotizPrompt.Should().Be(SummaryPrompts.Gespraechsnotiz);
        result.Settings.StundenzettelPrompt.Should().Be(SummaryPrompts.Stundenzettel);
        result.Settings.AnalogPrompt.Should().Be(SummaryPrompts.Analog);
    }

    [Fact]
    public void ApplyIfNeeded_WhenOldRevisionAndNoFileExists_MigratesWithoutBackup()
    {
        var settings = AppSettings.Default with
        {
            PromptDefaultsRevision = 0,
            SystemMessage = "CUSTOM SYSTEM",
        };

        var result = PromptDefaultsMigration.ApplyIfNeeded(settings, _settingsFilePath);

        result.DidMigrate.Should().BeTrue();
        result.BackupPath.Should().BeNull();
        result.Settings.PromptDefaultsRevision.Should().Be(PromptDefaultsMigration.CurrentRevision);
        result.Settings.SystemMessage.Should().Be(SummaryPrompts.SystemMessage);
    }
}
