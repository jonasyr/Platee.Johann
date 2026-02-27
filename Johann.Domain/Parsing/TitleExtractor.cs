namespace Johann.Domain.Parsing;

public static class TitleExtractor
{
    private static readonly HashSet<string> TitleKeywords =
        new(StringComparer.OrdinalIgnoreCase) { "titel", "betreff" };

    private const int MaxTitleWords = 15;

    /// <summary>
    /// If words[cursor] is "Titel" or "Betreff", extracts up to 15 words
    /// until "Ende" is encountered. Advances cursor past consumed tokens.
    /// Returns null if no title keyword found at current cursor position.
    /// </summary>
    public static string? TryExtract(string[] words, ref int cursor)
    {
        if (cursor >= words.Length || !TitleKeywords.Contains(words[cursor]))
            return null;

        cursor++; // skip "Titel" / "Betreff"

        var tokens = new List<string>(MaxTitleWords);
        while (cursor < words.Length && tokens.Count < MaxTitleWords)
        {
            if (words[cursor].Equals("ende", StringComparison.OrdinalIgnoreCase))
            {
                cursor++; // skip "Ende"
                break;
            }
            tokens.Add(words[cursor++]);
        }

        return tokens.Count > 0 ? string.Join(" ", tokens) : null;
    }
}
