namespace Platee.Johann.UiDriver.Sandbox;

using System.Text.Json;
using System.Text.Json.Nodes;

public static class TestSandbox
{
    public static SandboxLayout Create(string root, string? lastSeenReleaseNotesVersion, Func<JsonObject, JsonObject>? adjust = null) =>
        Create(root, lastSeenReleaseNotesVersion, SandboxGuard.DefaultForbiddenRoots(), adjust);

    public static SandboxLayout Create(
        string root,
        string? lastSeenReleaseNotesVersion,
        IEnumerable<string> forbiddenRoots,
        Func<JsonObject, JsonObject>? adjust)
    {
        // Must run before the first write — nothing may be created for a root the guard would reject anyway.
        SandboxGuard.EnsureRootOutsideForbiddenAreas(root, forbiddenRoots);

        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
        {
            throw new InvalidOperationException($"Sandbox-Wurzel '{root}' existiert bereits und ist nicht leer.");
        }

        var layout = new SandboxLayout(root);

        Directory.CreateDirectory(layout.Home);
        Directory.CreateDirectory(layout.Archiv);
        Directory.CreateDirectory(layout.Output);
        Directory.CreateDirectory(TeamDirectoryOf(layout));

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
            settings.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));

        File.WriteAllText(Path.Combine(layout.Home, ".env"), "OPENAI_API_KEY=sk-stub-not-a-real-key");

        SandboxGuard.Ensure(layout);

        return layout;
    }

    private static string TeamDirectoryOf(SandboxLayout layout) =>
        Path.GetDirectoryName(layout.TeamPrompts)
        ?? throw new InvalidOperationException($"Kein Verzeichnis für '{layout.TeamPrompts}' ermittelbar.");
}
