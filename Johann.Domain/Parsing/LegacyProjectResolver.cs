using System.Text.RegularExpressions;

namespace Johann.Domain.Parsing;

/// <summary>
/// Replicates the 7 regex patterns from the original Python summarizer.py
/// extract_project_from_transcript() function.
/// </summary>
public static class LegacyProjectResolver
{
    // Patterns ordered from most to least specific — first match wins.
    private static readonly Regex[] Patterns =
    [
        new(@"(?:das\s+)?projekt\s+ist\s+([a-zA-ZäöüÄÖÜß0-9\-_]+)", RegexOptions.IgnoreCase),
        new(@"projekt:\s*([a-zA-ZäöüÄÖÜß0-9\-_]+)",                  RegexOptions.IgnoreCase),
        new(@"für\s+(?:das\s+)?projekt\s+([a-zA-ZäöüÄÖÜß0-9\-_]+)", RegexOptions.IgnoreCase),
        new(@"zum\s+(?:das\s+)?projekt\s+([a-zA-ZäöüÄÖÜß0-9\-_]+)", RegexOptions.IgnoreCase),
        new(@"im\s+(?:das\s+)?projekt\s+([a-zA-ZäöüÄÖÜß0-9\-_]+)",  RegexOptions.IgnoreCase),
        new(@"beim\s+(?:das\s+)?projekt\s+([a-zA-ZäöüÄÖÜß0-9\-_]+)",RegexOptions.IgnoreCase),
        new(@"projekt\s+([a-zA-ZäöüÄÖÜß0-9\-_]+)",                   RegexOptions.IgnoreCase),
    ];

    public const string Fallback = "Allgemein";

    public static string Resolve(string transcript)
    {
        foreach (var pattern in Patterns)
        {
            var match = pattern.Match(transcript);
            if (match.Success)
            {
                var name = match.Groups[1].Value;
                // Title-case like Python's .title()
                return char.ToUpper(name[0]) + name[1..].ToLower();
            }
        }
        return Fallback;
    }
}
