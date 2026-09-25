namespace Platee.Johann.UiDriver.Tool;

/// <summary>
/// Minimal, dependency-free parsing for the handful of flag/option/positional shapes the
/// <c>ui-driver</c> commands use — not a general CLI framework, just enough to keep
/// <see cref="Commands"/> readable.
/// </summary>
public static class CommandArgs
{
    public static string? Option(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.Ordinal))
            {
                return args[i + 1];
            }
        }

        return null;
    }

    public static string RequireOption(string[] args, string name) =>
        Option(args, name) ?? throw new ArgumentException($"Fehlende Option '{name}'.");

    public static bool Flag(string[] args, string name) => args.Contains(name, StringComparer.Ordinal);

    /// <summary>
    /// Returns every token that is neither one of <paramref name="valueOptionNames"/> nor its
    /// value, nor a bare "--flag" — in order, so callers can index into it positionally.
    /// </summary>
    public static string[] PositionalsExcluding(string[] args, params string[] valueOptionNames)
    {
        var result = new List<string>();
        var i = 0;
        while (i < args.Length)
        {
            var token = args[i];
            if (Array.IndexOf(valueOptionNames, token) >= 0)
            {
                i += 2;
                continue;
            }

            if (token.StartsWith("--", StringComparison.Ordinal))
            {
                i += 1;
                continue;
            }

            result.Add(token);
            i += 1;
        }

        return [.. result];
    }

    public static string RequireAt(string[] positionals, int index, string description) =>
        index < positionals.Length
            ? positionals[index]
            : throw new ArgumentException($"Fehlendes Argument: {description}.");
}
