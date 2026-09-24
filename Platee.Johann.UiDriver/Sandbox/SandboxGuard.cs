namespace Platee.Johann.UiDriver.Sandbox;

using System.Text.Json;

public static class SandboxGuard
{
    private static readonly string[] RequiredFolderKeys =
        ["quellverzeichnis", "archivverzeichnis", "ausgabeverzeichnis"];

    public static IReadOnlyList<string> DefaultForbiddenRoots() =>
    [
        @"Z:\",
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann"),
    ];

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
                .SelectMany(s => roots
                    .Where(r => Normalise(s).Contains(r, StringComparison.OrdinalIgnoreCase))
                    .Select(r => $"'{s}' zeigt auf verbotenen Bereich '{r}'"))
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

    private static bool HasNonEmptyStringProperty(JsonElement root, string key) =>
        root.ValueKind == JsonValueKind.Object
        && root.TryGetProperty(key, out var value)
        && value.ValueKind == JsonValueKind.String
        && !string.IsNullOrWhiteSpace(value.GetString());

    private static string Normalise(string path) => path.Replace('/', '\\').TrimEnd('\\') + "\\";

    private static IEnumerable<string> Strings(JsonElement e) => e.ValueKind switch
    {
        JsonValueKind.String => [e.GetString()!],
        JsonValueKind.Object => e.EnumerateObject().SelectMany(p => Strings(p.Value)),
        JsonValueKind.Array => e.EnumerateArray().SelectMany(Strings),
        _ => [],
    };
}
