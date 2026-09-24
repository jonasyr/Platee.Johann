namespace Platee.Johann.UiDriver.Sandbox;

using System.Runtime.InteropServices;
using System.Text;
using System.Text.Json;

public static class SandboxGuard
{
    private static readonly string[] RequiredFolderKeys =
        ["quellverzeichnis", "archivverzeichnis", "ausgabeverzeichnis"];

    public static IReadOnlyList<string> DefaultForbiddenRoots()
    {
        var roots = new List<string>
        {
            @"Z:\",
            Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann"),
        };

        if (TryResolveUncPath("Z:", out var unc))
        {
            roots.Add(unc);
        }

        return roots;
    }

    public static IReadOnlyList<string> Violations(string settingsJson, IEnumerable<string> forbiddenRoots)
    {
        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(settingsJson);
        }
        catch (JsonException ex)
        {
            return [$"settings.json nicht lesbar: {ex.Message}"];
        }

        using (doc)
        {
            var roots = forbiddenRoots.Select(Normalise).ToArray();
            var forbiddenPathViolations = Strings(doc.RootElement)
                .SelectMany(s => CandidateForms(s)
                    .SelectMany(form => roots
                        .Where(r => Normalise(form).Contains(r, StringComparison.OrdinalIgnoreCase))
                        .Select(r => $"'{s}' zeigt auf verbotenen Bereich '{r}'")))
                .Distinct()
                .ToArray();

            var missingKeyViolations = RequiredFolderKeys
                .Where(key => !HasNonEmptyStringProperty(doc.RootElement, key))
                .Select(key => $"'{key}' fehlt oder ist leer")
                .ToArray();

            return [.. forbiddenPathViolations, .. missingKeyViolations];
        }
    }

    public static void Ensure(SandboxLayout layout)
    {
        var violations = Violations(File.ReadAllText(layout.SettingsFile), DefaultForbiddenRoots());
        if (violations.Count > 0)
        {
            throw new InvalidOperationException(
                "Sandbox-Wächter: Start verweigert." + Environment.NewLine + string.Join(Environment.NewLine, violations));
        }
    }

    /// <summary>
    /// Refuses a sandbox root that lies inside one of the forbidden areas, before anything is written to it.
    /// </summary>
    public static void EnsureRootOutsideForbiddenAreas(string root, IEnumerable<string> forbiddenRoots)
    {
        var fullRoot = NormaliseFullPath(root);
        var hit = forbiddenRoots.FirstOrDefault(r => fullRoot.StartsWith(NormaliseFullPath(r), StringComparison.OrdinalIgnoreCase));
        if (hit is not null)
        {
            throw new InvalidOperationException($"Sandbox-Wurzel '{root}' liegt im verbotenen Bereich '{hit}'.");
        }
    }

    /// <summary>
    /// Refuses a sandbox root that overlaps (in either direction) with another real filesystem path,
    /// so a sandbox can never be created inside — or wrap around — the data it is meant to isolate from.
    /// </summary>
    public static void EnsureNoOverlap(string root, string otherPath, string beschreibung)
    {
        var a = NormaliseFullPath(root);
        var b = NormaliseFullPath(otherPath);
        if (a.StartsWith(b, StringComparison.OrdinalIgnoreCase) || b.StartsWith(a, StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"Sandbox-Wurzel '{root}' überschneidet sich mit {beschreibung} ('{otherPath}').");
        }
    }

    private static bool HasNonEmptyStringProperty(JsonElement root, string key) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString());

    private static string Normalise(string path) => path.Replace('/', '\\').TrimEnd('\\') + "\\";

    private static string NormaliseFullPath(string path) => Normalise(Path.GetFullPath(path));

    private static IEnumerable<string> CandidateForms(string value)
    {
        yield return value;

        if (!Path.IsPathRooted(value))
        {
            yield break;
        }

        string? full = null;
        try
        {
            full = Path.GetFullPath(value);
        }
        catch (Exception)
        {
            // Not a valid filesystem path (illegal characters, too long, ...) — the raw form above still gets checked.
        }

        if (full is not null && !string.Equals(full, value, StringComparison.OrdinalIgnoreCase))
        {
            yield return full;
        }
    }

    private static IEnumerable<string> Strings(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => [e.GetString() ?? string.Empty],
        JsonValueKind.Object => e.EnumerateObject().SelectMany(p => Strings(p.Value)),
        JsonValueKind.Array => e.EnumerateArray().SelectMany(Strings),
        _ => [],
    };

    [DllImport("mpr.dll", CharSet = CharSet.Unicode)]
    private static extern int WNetGetConnection(string localName, StringBuilder remoteName, ref int length);

    private static bool TryResolveUncPath(string driveLetter, out string uncPath)
    {
        var sb = new StringBuilder(512);
        var length = sb.Capacity;
        try
        {
            if (WNetGetConnection(driveLetter, sb, ref length) == 0 && sb.Length > 0)
            {
                uncPath = sb.ToString();
                return true;
            }
        }
        catch (Exception)
        {
            // No such mapping, no networking stack, non-Windows, CI without a mapped drive — best effort only.
        }

        uncPath = string.Empty;
        return false;
    }
}
