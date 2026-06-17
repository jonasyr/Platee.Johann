namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;

public sealed class PromptDefaultsMigrationTests : IDisposable
{
    private readonly string tempDir;
    private readonly string promptsFilePath;

    public PromptDefaultsMigrationTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), $"JohannPromptMigrationTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.tempDir);
        this.promptsFilePath = Path.Combine(this.tempDir, "prompts.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(this.tempDir))
        {
            Directory.Delete(this.tempDir, recursive: true);
        }
    }

    [Fact]
    public void ApplyIfNeeded_WhenRevisionIsCurrent_DoesNothing()
    {
        var settings = PromptSettings.Default;

        var result = PromptDefaultsMigration.ApplyIfNeeded(settings, this.promptsFilePath);

        result.DidMigrate.Should().BeFalse();
        result.BackupPath.Should().BeNull();
        result.Settings.Should().BeEquivalentTo(settings);
    }

    [Fact]
    public void ApplyIfNeeded_WhenOldRevisionAndFileExists_BacksUpAndOverwritesOnlyPrompts()
    {
        File.WriteAllText(this.promptsFilePath, """{"systemMessage":"custom"}""");

        var settings = PromptSettings.Default with
        {
            PromptDefaultsRevision = 0,
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

        var result = PromptDefaultsMigration.ApplyIfNeeded(settings, this.promptsFilePath);

        result.DidMigrate.Should().BeTrue();
        result.BackupPath.Should().NotBeNull();
        File.Exists(result.BackupPath!).Should().BeTrue();
        File.ReadAllText(result.BackupPath!).Should().Contain("\"systemMessage\":\"custom\"");

        result.Settings.PromptDefaultsRevision.Should().Be(PromptDefaultsMigration.CurrentRevision);
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
        var settings = PromptSettings.Default with
        {
            PromptDefaultsRevision = 0,
            SystemMessage = "CUSTOM SYSTEM",
        };

        var result = PromptDefaultsMigration.ApplyIfNeeded(settings, this.promptsFilePath);

        result.DidMigrate.Should().BeTrue();
        result.BackupPath.Should().BeNull();
        result.Settings.PromptDefaultsRevision.Should().Be(PromptDefaultsMigration.CurrentRevision);
        result.Settings.SystemMessage.Should().Be(SummaryPrompts.SystemMessage);
    }
}
