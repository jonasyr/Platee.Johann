namespace Platee.Johann.Infrastructure.Llm;

/// <summary>
/// Resolves the OpenAI API key from environment variables or a .env file.
/// Search order:
///   1. OPENAI_API_KEY environment variable
///   2. Documents\Johann\.env  (stable user location, survives Velopack updates)
///   3. Walk up to 5 parent directories looking for a .env file
/// </summary>
public static class ApiKeyProvider
{
    public static string? TryGetOpenAiKey()
    {
        // 1. Environment variable (highest priority)
        var fromEnv = Environment.GetEnvironmentVariable("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv))
            return fromEnv.Trim();

        // 2. Documents\Johann\.env – easy to find, survives app updates
        var documentsEnv = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Johann", ".env");
        if (File.Exists(documentsEnv))
        {
            var key = ParseEnvFile(documentsEnv, "OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(key))
                return key;
        }

        // 3. Walk up parent directories looking for a .env file
        var dir = new DirectoryInfo(AppDomain.CurrentDomain.BaseDirectory);
        for (var i = 0; i < 5; i++)
        {
            if (dir is null) break;

            var envFile = Path.Combine(dir.FullName, ".env");
            if (File.Exists(envFile))
            {
                var key = ParseEnvFile(envFile, "OPENAI_API_KEY");
                if (!string.IsNullOrWhiteSpace(key))
                    return key;
            }

            dir = dir.Parent;
        }

        return null;
    }

    private static string? ParseEnvFile(string path, string keyName)
    {
        foreach (var line in File.ReadAllLines(path))
        {
            var trimmed = line.Trim();
            if (trimmed.StartsWith('#')) continue;                    // comment line
            if (!trimmed.StartsWith(keyName + "=", StringComparison.Ordinal)) continue;

            var value = trimmed[(keyName.Length + 1)..].Trim();
            return value.Trim('"', '\'');
        }

        return null;
    }
}
