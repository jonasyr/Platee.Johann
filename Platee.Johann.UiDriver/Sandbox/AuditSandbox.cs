namespace Platee.Johann.UiDriver.Sandbox;

using System.Text.Json.Nodes;

public static class AuditSandbox
{
    private static readonly string[] HomeFilesToCopy = ["settings.json", "prompts.personal.json", ".env"];

    public static SandboxLayout Create(string root, string realHome, string? teamPromptFile)
    {
        if (Directory.Exists(root) && Directory.EnumerateFileSystemEntries(root).Any())
        {
            throw new InvalidOperationException($"Sandbox-Wurzel '{root}' existiert bereits und ist nicht leer.");
        }

        var layout = new SandboxLayout(root);

        Directory.CreateDirectory(layout.Home);
        Directory.CreateDirectory(layout.Archiv);
        Directory.CreateDirectory(layout.Output);
        Directory.CreateDirectory(Path.GetDirectoryName(layout.TeamPrompts)!);

        foreach (var fileName in HomeFilesToCopy)
        {
            var source = Path.Combine(realHome, fileName);
            if (File.Exists(source))
            {
                File.Copy(source, Path.Combine(layout.Home, fileName), overwrite: true);
            }
        }

        var realOutput = Path.Combine(realHome, "output");
        if (Directory.Exists(realOutput))
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

    private static void RedirectSettings(SandboxLayout layout)
    {
        var node = File.Exists(layout.SettingsFile)
            ? JsonNode.Parse(File.ReadAllText(layout.SettingsFile))!.AsObject()
            : new JsonObject();

        node["quellverzeichnis"] = layout.Eingang;
        node["archivverzeichnis"] = layout.Archiv;
        node["ausgabeverzeichnis"] = layout.Output;
        node["globalPromptFilePath"] = layout.TeamPrompts;

        File.WriteAllText(layout.SettingsFile, node.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
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
