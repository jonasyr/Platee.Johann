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

        var startCursor = cursor;
        cursor++; // skip "Titel" / "Betreff"

        var tokens = new List<string>(MaxTitleWords);

        // Peak ahead up to 35 words to check if "Ende" is used too late (Rule 5)
        var limitCheck = Math.Min(cursor + 35, words.Length);
        var endeIndex = -1;
        for (int i = cursor; i < limitCheck; i++)
        {
            if (words[i].Equals("ende", StringComparison.OrdinalIgnoreCase))
            {
                endeIndex = i;
                break;
            }
        }

        // Rule 5: If "Ende" is found but strictly > 15 words away (i.e. late), we treat it as no title and let GPT do it.
        // We will just return null and skip over "Titel" and everything up to "Ende" so the text is still stripped,
        // but since we want GPT to title it, maybe we *shouldn't* skip? Wait, let's just abort extraction.
        if (endeIndex != -1 && (endeIndex - cursor) > MaxTitleWords)
        {
            cursor = endeIndex + 1; // skip everything including "ende" so it's not in the prose
            return null; // GPT will generate
        }

        while (cursor < words.Length && tokens.Count < MaxTitleWords)
        {
            if (words[cursor].Equals("ende", StringComparison.OrdinalIgnoreCase))
            {
                cursor++; // skip "Ende"
                break;
            }
            tokens.Add(words[cursor++]);
        }

        // Rule 3: If "Ende" forgotten, ends automatically after 15 words.
        return tokens.Count > 0 ? string.Join(" ", tokens) : null;
    }
}
