namespace Platee.Johann.Application.Settings;

using System.IO;
using Platee.Johann.Application.Processing;

public static class PromptDefaultsMigration
{
    public const int CurrentRevision = 20260513;

    public static PromptDefaultsMigrationResult ApplyIfNeeded(PromptSettings settings, string promptsFilePath)
    {
        if (settings.PromptDefaultsRevision >= CurrentRevision)
        {
            return new(settings, null, false);
        }

        string? backupPath = null;
        if (File.Exists(promptsFilePath))
        {
            backupPath = BuildBackupPath(promptsFilePath);
            File.Copy(promptsFilePath, backupPath, overwrite: false);
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

    private static string BuildBackupPath(string promptsFilePath)
    {
        var directory = Path.GetDirectoryName(promptsFilePath) ?? string.Empty;
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmssfff");
        return Path.Combine(directory, $"prompts.backup-{timestamp}.json.bak");
    }
}

public sealed record PromptDefaultsMigrationResult(
    PromptSettings Settings,
    string? BackupPath,
    bool DidMigrate);
