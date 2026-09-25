namespace Platee.Johann.Domain.Services;

using System.Text;

/// <summary>
/// Puts every sentence of a transcript on its own line, for display only (#112).
/// <para>
/// The stored transcript is the literal record of what was said and is never changed; this runs
/// when it is shown (detail view, PDF, HTML). A break needs a sentence end (<c>.</c>, <c>!</c>,
/// <c>?</c>), optionally followed by closing quotes or brackets, then whitespace, then a capital
/// letter or an opening quote. After a dot it is additionally suppressed for abbreviations,
/// initials, numbers (dates, ordinals, times), ellipses and dotted short forms like „z.B.“ —
/// a missed break only costs readability, a wrong one splits a sentence, so doubt means no break.
/// </para>
/// </summary>
public static class SentenceLines
{
    private const string ClosingChars = "\"'“”’»)]";
    private const string OpeningChars = "\"'„“‚«([";

    // "usw." and "etc." are deliberately absent: they almost always end a sentence, and when they
    // do not, the next word is lowercase and no break happens anyway.
    private static readonly HashSet<string> Abbreviations = new(StringComparer.OrdinalIgnoreCase)
    {
        "abs", "bspw", "bzw", "ca", "dr", "evtl", "fr", "ggf", "hr", "inkl", "ing", "max",
        "min", "mio", "mrd", "mr", "mrs", "nr", "prof", "st", "str", "tel", "vgl", "zzgl",
        "e.g", "i.e", "vs",
    };

    public static string Split(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return text;
        }

        var sb = new StringBuilder(text.Length + 16);
        var i = 0;
        while (i < text.Length)
        {
            var c = text[i];
            if (c is not ('.' or '!' or '?'))
            {
                sb.Append(c);
                i++;
                continue;
            }

            // The whole run of end punctuation ("?!", "...") plus closing quotes/brackets.
            var runStart = i;
            while (i < text.Length && text[i] is '.' or '!' or '?')
            {
                i++;
            }

            var punctuation = text[runStart..i];
            while (i < text.Length && ClosingChars.Contains(text[i]))
            {
                i++;
            }

            sb.Append(text, runStart, i - runStart);

            var gapStart = i;
            while (i < text.Length && char.IsWhiteSpace(text[i]))
            {
                i++;
            }

            var gap = text[gapStart..i];
            if (gap.Length > 0 && !gap.Contains('\n')
                && StartsSentence(text, i)
                && !SuppressedAfter(text, runStart, punctuation))
            {
                sb.Append('\n');
            }
            else
            {
                sb.Append(gap);
            }
        }

        return sb.ToString();
    }

    private static bool StartsSentence(string text, int index)
    {
        while (index < text.Length && OpeningChars.Contains(text[index]))
        {
            index++;
        }

        return index < text.Length && char.IsUpper(text[index]);
    }

    private static bool SuppressedAfter(string text, int punctuationStart, string punctuation)
    {
        if (punctuation != ".")
        {
            // "!" and "?" always end a sentence; "..." is an ellipsis, not an end.
            return punctuation.All(p => p == '.');
        }

        var tokenStart = punctuationStart;
        while (tokenStart > 0 && !char.IsWhiteSpace(text[tokenStart - 1]) && !OpeningChars.Contains(text[tokenStart - 1]))
        {
            tokenStart--;
        }

        var token = text[tokenStart..punctuationStart];
        if (token.Length == 0)
        {
            return false;
        }

        // Numbers: dates (24.09.), ordinals (3.), times (10.30) — ambiguous, so no break.
        if (char.IsDigit(token[^1]))
        {
            return true;
        }

        // Initials (M. Müller) and single-letter parts of „z. B.“.
        if (token.Length == 1 && char.IsLetter(token[0]))
        {
            return true;
        }

        if (Abbreviations.Contains(token))
        {
            return true;
        }

        // Dotted short forms (z.B, u.a, d.h) — but not domains or addresses (peano.de).
        return token.Contains('.') && !token.Contains('@') && !token.Contains('/')
            && token.Split('.').All(part => part.Length is > 0 and <= 2 && part.All(char.IsLetter));
    }
}
