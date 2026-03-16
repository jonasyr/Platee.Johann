namespace Johann.Domain.Parsing;

public static class TitleExtractor
{
    private static readonly HashSet<string> TitleKeywords =
        new(StringComparer.OrdinalIgnoreCase) { "titel", "betreff" };

    private static readonly char[] PunctuationChars = [' ', ',', '.', ':', ';', '!', '?'];

    private const int MaxTitleWords = 15;

    // "Ende" appearing 30+ words after the title keyword is treated as
    // "way too late" — the user rambled; let GPT generate the title instead.
    private const int LateEndeThreshold = 30;

    /// <summary>
    /// If words[cursor] is "Titel" or "Betreff", extracts up to 15 words
    /// until "Ende" is encountered. Advances cursor past consumed tokens.
    /// Returns null if no title keyword found at current cursor position.
    ///
    /// Rules:
    ///   – "Ende" within 15 words  → stop there, use those words as title.
    ///   – "Ende" at 16–29 words   → cap at 15 words, then skip "Ende".
    ///   – "Ende" at 30+ words     → return null (GPT generates title).
    ///   – No "Ende" at all        → cap at 15 words.
    /// </summary>
    public static string? TryExtract(string[] words, ref int cursor)
    {
        if (cursor >= words.Length)
            return null;

        var currentToken = words[cursor].Trim(PunctuationChars);
        if (!TitleKeywords.Contains(currentToken))
            return null;

        cursor++; // skip "Titel" / "Betreff"

        // Search for "Ende" within 35 words (beyond that it cannot affect extraction)
        var searchLimit = Math.Min(cursor + 35, words.Length);
        var endeIndex = -1;
        for (int i = cursor; i < searchLimit; i++)
        {
            if (words[i].Trim(PunctuationChars).Equals("ende", StringComparison.OrdinalIgnoreCase))
            {
                endeIndex = i;
                break;
            }
        }

        // Rule 5: "Ende" found 30+ words away → too late, let GPT generate.
        if (endeIndex != -1 && (endeIndex - cursor) >= LateEndeThreshold)
        {
            cursor = endeIndex + 1; // consume everything up to and including "Ende"
            return null;
        }

        // Collect up to 15 words, stopping at "Ende" if it arrives in time.
        var tokens = new List<string>(MaxTitleWords);
        var endeFound = false;
        while (cursor < words.Length && tokens.Count < MaxTitleWords)
        {
            var token = words[cursor].Trim(PunctuationChars);
            if (token.Equals("ende", StringComparison.OrdinalIgnoreCase))
            {
                cursor++; // skip "Ende"
                endeFound = true;
                break;
            }
            tokens.Add(words[cursor++]);
        }

        // "Ende" was found 16–29 words away but we already capped at 15 words — skip it
        // so it does not appear in the remainder / transcript body.
        if (!endeFound && endeIndex != -1)
        {
            cursor = endeIndex + 1;
        }

        // Rule 3: If "Ende" forgotten, ends automatically after 15 words.
        return tokens.Count > 0 ? string.Join(" ", tokens).Trim(PunctuationChars) : null;
    }
}
