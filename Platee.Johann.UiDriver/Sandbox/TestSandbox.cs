namespace Platee.Johann.UiDriver.Sandbox;

using System.Text.Json.Nodes;

public static class TestSandbox
{
    public static SandboxLayout Create(string root, string? lastSeenReleaseNotesVersion, Func<JsonObject, JsonObject>? adjust = null)
    {
        var layout = new SandboxLayout(root);

        Directory.CreateDirectory(layout.Home);
        Directory.CreateDirectory(layout.Archiv);
        Directory.CreateDirectory(layout.Output);
        Directory.CreateDirectory(Path.GetDirectoryName(layout.TeamPrompts)!);

        var settings = new JsonObject
        {
            ["name"] = "UI-Test",
            ["firma"] = "Test GmbH",
            ["quellverzeichnis"] = layout.Eingang,
            ["archivverzeichnis"] = layout.Archiv,
            ["ausgabeverzeichnis"] = layout.Output,
            ["globalPromptFilePath"] = string.Empty,
            ["sectionModesMigrationDone"] = true,
            ["summaryModel"] = "gpt-5.6-luna",
        };

        if (lastSeenReleaseNotesVersion is not null)
        {
            settings["lastSeenReleaseNotesVersion"] = lastSeenReleaseNotesVersion;
        }

        if (adjust is not null)
        {
            settings = adjust(settings);
        }

        File.WriteAllText(
            layout.SettingsFile,
            settings.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));

        File.WriteAllText(Path.Combine(layout.Home, ".env"), "OPENAI_API_KEY=sk-stub-not-a-real-key");

        SandboxGuard.Ensure(layout);

        return layout;
    }
}
