using System.Text.Json;
using System.Text.Json.Nodes;

namespace Platee.Johann.Application.Settings;

public sealed record SettingsSplitMigrationResult(bool DidMigrate);

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
        catch
        {
            return new(false);
        }
    }
}
