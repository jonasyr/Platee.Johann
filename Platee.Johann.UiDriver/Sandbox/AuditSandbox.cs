namespace Platee.Johann.UiDriver.Sandbox;

using System.Text.Json;
using System.Text.Json.Nodes;

public static class AuditSandbox
{
    private static readonly string[] HomeFilesToCopy = ["settings.json", "prompts.personal.json", ".env"];

    public static SandboxLayout Create(string root, string realHome, string? teamPromptFile) =>
        Create(root, realHome, teamPromptFile, SandboxGuard.DefaultForbiddenRoots());

    public static SandboxLayout Create(string root, string realHome, string? teamPromptFile, IEnumerable<string> forbiddenRoots)
    {
        // Every safety check below must run before the first write (CreateDirectory/Copy) —
        // a root inside a forbidden area or inside the very data it copies from must never
        // touch the filesystem.
        SandboxGuard.EnsureRootOutsideForbiddenAreas(root, forbiddenRoots);
        SandboxGuard.EnsureNoOverlap(root, realHome, "dem echten Home-Verzeichnis");

        var realOutput = ResolveRealOutput(realHome);
        var realOutputExists = Directory.Exists(realOutput);
        if (realOutputExists)
        {
            SandboxGuard.EnsureNoOverlap(root, realOutput, "dem echten Ausgabeverzeichnis");
        }

        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
        {
            throw new InvalidOperationException($"Sandbox-Wurzel '{root}' existiert bereits und ist nicht leer.");
        }

        var layout = new SandboxLayout(root);

        Directory.CreateDirectory(layout.Home);
        Directory.CreateDirectory(layout.Archiv);
        Directory.CreateDirectory(layout.Output);
        Directory.CreateDirectory(TeamDirectoryOf(layout));

        foreach (var fileName in HomeFilesToCopy)
        {
            var source = Path.Combine(realHome, fileName);
            if (File.Exists(source))
            {
                File.Copy(source, Path.Combine(layout.Home, fileName), overwrite: true);
            }
        }

        if (realOutputExists)
        {
            CopyDirectoryRecursive(realOutput, layout.Output);
        }

        if (teamPromptFile is not null && File.Exists(teamPromptFile))
        {
            File.Copy(teamPromptFile, layout.TeamPrompts, overwrite: true);
        }
        else
        {
            File.WriteAllText(layout.TeamPrompts, "{}");
        }

        RedirectSettings(layout);

        SandboxGuard.Ensure(layout);

        return layout;
    }

    /// <summary>
    /// Reads (read-only) which output folder the real settings.json actually points at, falling
    /// back to "&lt;realHome&gt;\output" when the file, the key, or the folder itself is missing.
    /// </summary>
    private static string ResolveRealOutput(string realHome)
    {
        var fallback = Path.Combine(realHome, "output");
        var settingsPath = Path.Combine(realHome, "settings.json");
        if (!File.Exists(settingsPath))
        {
            return fallback;
        }

        try
        {
            var node = JsonNode.Parse(File.ReadAllText(settingsPath));
            var configured = node?["ausgabeverzeichnis"]?.GetValue<string>();
            if (!string.IsNullOrWhiteSpace(configured) && Directory.Exists(configured))
            {
                return configured;
            }
        }
        catch (JsonException)
        {
            // A corrupt real settings.json is not this audit's concern — fall back below.
        }

        return fallback;
    }

    private static string TeamDirectoryOf(SandboxLayout layout) =>
        Path.GetDirectoryName(layout.TeamPrompts)
        ?? throw new InvalidOperationException($"Kein Verzeichnis für '{layout.TeamPrompts}' ermittelbar.");

    private static void RedirectSettings(SandboxLayout layout)
    {
        JsonObject node;
        if (File.Exists(layout.SettingsFile))
        {
            var parsed = JsonNode.Parse(File.ReadAllText(layout.SettingsFile))
                ?? throw new InvalidOperationException($"'{layout.SettingsFile}' enthält kein gültiges JSON-Objekt.");
            node = parsed.AsObject();
        }
        else
        {
            node = new JsonObject();
        }

        node["quellverzeichnis"] = layout.Eingang;
        node["archivverzeichnis"] = layout.Archiv;
        node["ausgabeverzeichnis"] = layout.Output;
        node["globalPromptFilePath"] = layout.TeamPrompts;

        File.WriteAllText(layout.SettingsFile, node.ToJsonString(new JsonSerializerOptions { WriteIndented = true }));
    }

    private static void CopyDirectoryRecursive(string sourceDir, string targetDir)
    {
        Directory.CreateDirectory(targetDir);

        foreach (var file in Directory.EnumerateFiles(sourceDir))
        {
            var info = new FileInfo(file);
            if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                continue;
            }

            var fileName = Path.GetFileName(file);
            if (fileName.StartsWith("prompts.cache.", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            File.Copy(file, Path.Combine(targetDir, fileName), overwrite: true);
        }

        foreach (var dir in Directory.EnumerateDirectories(sourceDir))
        {
            var info = new DirectoryInfo(dir);
            if (info.Attributes.HasFlag(FileAttributes.ReparsePoint))
            {
                continue;
            }

            CopyDirectoryRecursive(dir, Path.Combine(targetDir, Path.GetFileName(dir)));
        }
    }
}
