namespace Platee.Johann.UiTests;

/// <summary>
/// Finds the real <c>Platee.Johann.UI.exe</c> the UI tests drive: walks up from the test
/// assembly's own output folder until it finds <c>Platee.Johann.slnx</c> (the repo root), then
/// looks for the built exe under <c>Platee.Johann.UI/bin/&lt;Configuration&gt;/net10.0-windows/</c>.
/// The path-walking (<see cref="FindSolutionRoot"/>) is pure and unit-tested in
/// <c>Platee.Johann.Tests</c> via a linked copy of this file; the actual file-existence check
/// stays here since it needs a real build to pass.
/// </summary>
public static class ExeLocator
{
    private const string SolutionFileName = "Platee.Johann.slnx";
    private const string ExeRelativeDir = "Platee.Johann.UI";
    private const string ExeName = "Platee.Johann.UI.exe";
    private const string TargetFramework = "net10.0-windows";

#if DEBUG
    private const string DefaultConfiguration = "Debug";
#else
    private const string DefaultConfiguration = "Release";
#endif

    public static string Find(string? startDirectory = null, string? configuration = null)
    {
        var root = startDirectory ?? AppContext.BaseDirectory;
        var solutionRoot = FindSolutionRoot(root)
            ?? throw new InvalidOperationException(
                $"'{SolutionFileName}' nicht gefunden oberhalb von '{root}' — läuft dieser Test innerhalb des Repos?");

        var exePath = Path.Combine(
            solutionRoot,
            ExeRelativeDir,
            "bin",
            configuration ?? DefaultConfiguration,
            TargetFramework,
            ExeName);

        if (!File.Exists(exePath))
        {
            throw new FileNotFoundException(
                $"Johann-EXE nicht gefunden unter '{exePath}' — erst 'dotnet build' ausführen.",
                exePath);
        }

        return exePath;
    }

    /// <summary>
    /// Walks up from <paramref name="startDirectory"/> looking for <c>Platee.Johann.slnx</c>,
    /// returning the directory that contains it, or <see langword="null"/> if none of the
    /// ancestors has it. Pure filesystem lookup — no exceptions on a normal miss.
    /// </summary>
    public static string? FindSolutionRoot(string startDirectory)
    {
        var directory = new DirectoryInfo(startDirectory);
        while (directory is not null)
        {
            if (File.Exists(Path.Combine(directory.FullName, SolutionFileName)))
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        return null;
    }
}
