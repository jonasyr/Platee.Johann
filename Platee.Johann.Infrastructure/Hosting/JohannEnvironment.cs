namespace Platee.Johann.Infrastructure.Hosting;

/// <summary>
/// Liest die Umgebungsvariablen, mit denen sich Johann für Automationsläufe umlenken lässt (#111).
/// Ungesetzt oder leer gilt das heutige Verhalten. Gesetzt und ungültig wirft — ein stiller
/// Rückfall landete bei einem Tippfehler in den echten Daten oder bei der bezahlten API.
/// </summary>
public static class JohannEnvironment
{
    public const string HomeVariable = "JOHANN_HOME";
    public const string OpenAiEndpointVariable = "JOHANN_OPENAI_ENDPOINT";
    public const string NoUpdateCheckVariable = "JOHANN_NO_UPDATE_CHECK";

    public static string HomeDirectory(Func<string, string?>? read = null)
    {
        var raw = Clean((read ?? Environment.GetEnvironmentVariable)(HomeVariable));
        if (raw is null)
        {
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann");
        }

        string full;
        try
        {
            if (!Path.IsPathFullyQualified(raw) || raw.StartsWith(@"\\?\"))
            {
                throw new ArgumentException("kein absoluter Pfad");
            }

            full = Path.GetFullPath(raw);
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            throw new InvalidOperationException(
                $"{HomeVariable} ist gesetzt, aber kein gültiger absoluter Ordner: '{raw}'.", ex);
        }

        var root = Path.GetPathRoot(full);
        return string.Equals(full, root, StringComparison.OrdinalIgnoreCase)
            ? full
            : full.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
    }

    public static bool HasHomeOverride(Func<string, string?>? read = null) =>
        Clean((read ?? Environment.GetEnvironmentVariable)(HomeVariable)) is not null;

    public static Uri? OpenAiRoot(Func<string, string?>? read = null)
    {
        var raw = Clean((read ?? Environment.GetEnvironmentVariable)(OpenAiEndpointVariable));
        if (raw is null)
        {
            return null;
        }

        if (!Uri.TryCreate(raw, UriKind.Absolute, out var uri)
            || (uri.Scheme != Uri.UriSchemeHttp && uri.Scheme != Uri.UriSchemeHttps))
        {
            throw new InvalidOperationException(
                $"{OpenAiEndpointVariable} ist gesetzt, aber keine http(s)-Adresse: '{raw}'.");
        }

        return uri.AbsoluteUri.EndsWith('/') ? uri : new Uri(uri.AbsoluteUri + "/");
    }

    public static bool SkipUpdateCheck(Func<string, string?>? read = null)
    {
        var raw = Clean((read ?? Environment.GetEnvironmentVariable)(NoUpdateCheckVariable));
        return raw is "1" || string.Equals(raw, "true", StringComparison.OrdinalIgnoreCase);
    }

    private static string? Clean(string? value)
    {
        var trimmed = value?.Trim().Trim('"', '\'').Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }
}
