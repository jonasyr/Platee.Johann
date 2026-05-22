namespace Platee.Johann.Application.Settings;

using System.IO;
using Platee.Johann.Application.Processing;

public static class PromptDefaultsMigration
{
    public const int CurrentRevision = 20260513;

    public static PromptDefaultsMigrationResult ApplyIfNeeded(AppSettings settings, string settingsFilePath)
    {
        if (settings.PromptDefaultsRevision >= CurrentRevision)
        {
            return new(settings, null, false);
        }

        string? backupPath = null;
        if (File.Exists(settingsFilePath))
        {
            backupPath = BuildBackupPath(settingsFilePath);
            File.Copy(settingsFilePath, backupPath, overwrite: false);
        }

        var migrated = settings with
        {
            PromptDefaultsRevision = CurrentRevision,
            SystemMessage = SummaryPrompts.SystemMessage,
            AbstractPrompt = SummaryPrompts.Abstract,
            StructuredPrompt = SummaryPrompts.Structured,
            ProsePrompt = SummaryPrompts.Prose,
            EmailPrompt = SummaryPrompts.Email,
            AufgabePrompt = SummaryPrompts.Aufgabe,
            GespraechsnotizPrompt = SummaryPrompts.Gespraechsnotiz,
            StundenzettelPrompt = SummaryPrompts.Stundenzettel,
            AnalogPrompt = SummaryPrompts.Analog,
        };

        return new(migrated, backupPath, true);
    }

    private static string BuildBackupPath(string settingsFilePath)
    {
        var directory = Path.GetDirectoryName(settingsFilePath) ?? string.Empty;
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
        return Path.Combine(directory, $"settings.prompts-backup-{timestamp}.json.bak");
    }
}

public sealed record PromptDefaultsMigrationResult(
    AppSettings Settings,
    string? BackupPath,
    bool DidMigrate);
