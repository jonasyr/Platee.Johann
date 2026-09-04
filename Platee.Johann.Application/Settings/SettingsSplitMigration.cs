using System.Text.Json;
using System.Text.Json.Nodes;

namespace Platee.Johann.Application.Settings;

/// <param name="Failure">
/// Why the migration could not run, or <c>null</c> when nothing went wrong.
/// Without this a crashed migration is indistinguishable from "nothing to do" (#45 M5).
/// </param>
public sealed record SettingsSplitMigrationResult(bool DidMigrate, string? Failure = null);

public static class SettingsSplitMigration
{
    private static readonly string[] PromptKeys =
    [
        "promptDefaultsRevision",
        "systemMessage",
        "abstractPrompt",
        "structuredPrompt",
        "prosePrompt",
        "emailPrompt",
        "aufgabePrompt",
        "gespraechsnotizPrompt",
        "stundenzettelPrompt",
        "analogPrompt",
    ];

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
    };

    /// <summary>
    /// Best-effort removal of legacy local prompt files.
    /// </summary>
    /// <returns>The error message if cleanup failed, otherwise <c>null</c> (#45 L1).</returns>
    public static string? CleanupLegacyFiles(string settingsDirectory)
    {
        try
        {
            // Remove local prompts.json if it exists
            var promptsPath = Path.Combine(settingsDirectory, "prompts.json");
            if (File.Exists(promptsPath))
            {
                File.Delete(promptsPath);
            }

            // Strip any remaining prompt keys from settings.json
            var settingsPath = Path.Combine(settingsDirectory, "settings.json");
            if (!File.Exists(settingsPath))
            {
                return null;
            }

            var json = File.ReadAllText(settingsPath);
            var root = JsonNode.Parse(json);
            if (root is not JsonObject settingsObj)
            {
                return null;
            }

            var hadPromptFields = false;
            foreach (var key in PromptKeys)
            {
                if (settingsObj.Remove(key))
                {
                    hadPromptFields = true;
                }
            }

            if (hadPromptFields)
            {
                File.WriteAllText(settingsPath, settingsObj.ToJsonString(WriteOptions));
            }

            return null;
        }
        catch (Exception ex)
        {
            // Cleanup must never break startup, but the caller still gets told.
            return ex.Message;
        }
    }

    public static SettingsSplitMigrationResult MigrateIfNeeded(string settingsFilePath, string promptsFilePath)
    {
        if (File.Exists(promptsFilePath))
        {
            return new(false);
        }

        if (!File.Exists(settingsFilePath))
        {
            return new(false);
        }

        try
        {
            var json = File.ReadAllText(settingsFilePath);
            var root = JsonNode.Parse(json);
            if (root is not JsonObject settingsObj)
            {
                return new(false);
            }

            var promptObj = new JsonObject();
            var hadPromptFields = false;

            foreach (var key in PromptKeys)
            {
                if (settingsObj.ContainsKey(key))
                {
                    promptObj[key] = settingsObj[key]?.DeepClone();
                    settingsObj.Remove(key);
                    hadPromptFields = true;
                }
            }

            if (!hadPromptFields)
            {
                return new(false);
            }

            File.WriteAllText(promptsFilePath, promptObj.ToJsonString(WriteOptions));
            File.WriteAllText(settingsFilePath, settingsObj.ToJsonString(WriteOptions));

            return new(true);
        }
        catch (Exception ex)
        {
            return new(false, ex.Message);
        }
    }
}
