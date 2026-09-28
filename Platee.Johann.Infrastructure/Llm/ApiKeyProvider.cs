namespace Platee.Johann.Infrastructure.Llm;

using Platee.Johann.Infrastructure.Hosting;

/// <summary>
/// Resolves the OpenAI API key from environment variables or a .env file.
/// Search order:
///   1. OPENAI_API_KEY environment variable
///   2. &lt;Johann-Home&gt;\.env  — Documents\Johann, or JOHANN_HOME when set (#111).
///   3. Walk up to 5 parent directories looking for a .env file. Skipped entirely when
///      JOHANN_HOME is set: a redirected sandbox must never fall back to a key it finds
///      next to the EXE (e.g. the repo checkout's own .env).
/// </summary>
public static class ApiKeyProvider
{
    public static string? TryGetOpenAiKey(Func<string, string?>? read = null, string? baseDirectory = null)
    {
        read ??= Environment.GetEnvironmentVariable;

        // 1. Environment variable (highest priority)
        var fromEnv = read("OPENAI_API_KEY");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv.Trim();
        }

        // 2. <Johann-Home>\.env – Documents\Johann, oder JOHANN_HOME (#111)
        var homeEnv = Path.Combine(JohannEnvironment.HomeDirectory(read), ".env");
        if (File.Exists(homeEnv))
        {
            var key = ParseEnvFile(homeEnv, "OPENAI_API_KEY");
            if (!string.IsNullOrWhiteSpace(key))
            {
                return key;
            }
        }

        // Eine umgelenkte Sandbox darf keinen Schlüssel aus dem Repo neben der EXE finden.
        if (JohannEnvironment.HasHomeOverride(read))
        {
            return null;
        }

        // 3. Walk up parent directories looking for a .env file
        var dir = new DirectoryInfo(baseDirectory ?? AppDomain.CurrentDomain.BaseDirectory);
        for (var i = 0; i < 5; i++)
        {
            if (dir is null)
            {
                break;
            }

            var envFile = Path.Combine(dir.FullName, ".env");
            if (File.Exists(envFile))
            {
                var key = ParseEnvFile(envFile, "OPENAI_API_KEY");
                if (!string.IsNullOrWhiteSpace(key))
                {
                    return key;
                }
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
            if (trimmed.StartsWith('#'))
            {
                continue;                    // comment line
            }

            if (!trimmed.StartsWith(keyName + "=", StringComparison.Ordinal))
            {
                continue;
            }

            var value = trimmed[(keyName.Length + 1)..].Trim();
            return value.Trim('"', '\'');
        }

        return null;
    }
}
