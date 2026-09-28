namespace Platee.Johann.UiDriver.Tool;

using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

/// <summary>
/// Persists which Johann process the last <c>start</c> command launched, so every later
/// invocation of the (short-lived, one-shot) <c>ui-driver</c> process can attach to the same
/// Johann instance — see %TEMP%\johann-ui-driver\session.json.
/// </summary>
internal static class SessionStore
{
    private static readonly string FilePath = Path.Combine(Path.GetTempPath(), "johann-ui-driver", "session.json");

    public static void Save(int pid, string root)
    {
        var directory = Path.GetDirectoryName(FilePath)
            ?? throw new InvalidOperationException($"Kein Verzeichnis für '{FilePath}' ermittelbar.");
        Directory.CreateDirectory(directory);
        File.WriteAllText(FilePath, JsonSerializer.Serialize(new { pid, root }));
    }

    public static void Clear()
    {
        if (File.Exists(FilePath))
        {
            File.Delete(FilePath);
        }
    }

    /// <summary>
    /// Reads the last saved PID and confirms the process is still alive.
    /// Throws with the exact message the CLI's error envelope must show when there is nothing to attach to.
    /// </summary>
    public static int LoadRunningPid()
    {
        if (!File.Exists(FilePath))
        {
            throw new InvalidOperationException("keine laufende Sitzung");
        }

        var pidNode = JsonNode.Parse(File.ReadAllText(FilePath))?["pid"];
        if (pidNode is null)
        {
            throw new InvalidOperationException("keine laufende Sitzung");
        }

        var pid = pidNode.GetValue<int>();
        try
        {
            Process.GetProcessById(pid);
        }
        catch (ArgumentException)
        {
            throw new InvalidOperationException("keine laufende Sitzung");
        }

        return pid;
    }
}
