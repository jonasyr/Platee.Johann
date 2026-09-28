namespace Platee.Johann.Domain.Services;

using System.Text.RegularExpressions;

/// <summary>A piece of one line with its emphasis.</summary>
public readonly record struct InlineRun(string Text, bool Bold, bool Italic);

/// <summary>
/// Bold (<c>**x**</c>) and italic (<c>*x*</c>) inside a line, for outputs that cannot hand the
/// text to a markdown engine: the PDF and plain-text copies. Since the generated sections are
/// asked for markdown (#73, S4), these would otherwise show literal asterisks.
/// <para>
/// Deliberately narrow: an asterisk only counts when it opens and closes a word-bounded span,
/// so "3 * 4" or "2*3" stay text.
/// </para>
/// </summary>
public static partial class InlineMarkdown
{
    /// <summary>Splits a line into runs of normal, bold and italic text.</summary>
    /// <param name="line">One line of markdown.</param>
    /// <returns>The runs in order; a line without emphasis is a single normal run.</returns>
    public static IReadOnlyList<InlineRun> Split(string line)
    {
        var runs = new List<InlineRun>();
        var position = 0;
        foreach (Match match in Emphasis().Matches(line))
        {
            if (match.Index > position)
            {
                runs.Add(new InlineRun(line[position..match.Index], false, false));
            }

            runs.Add(match.Groups["bold"].Success
                ? new InlineRun(match.Groups["bold"].Value, true, false)
                : new InlineRun(match.Groups["italic"].Value, false, true));
            position = match.Index + match.Length;
        }

        if (position < line.Length || runs.Count == 0)
        {
            runs.Add(new InlineRun(line[position..], false, false));
        }

        return runs;
    }

    /// <summary>Removes headings, bold and italic markers; list dashes and indentation stay.</summary>
    /// <param name="markdown">Markdown text.</param>
    /// <returns>Readable plain text with <c>\n</c> line endings.</returns>
    public static string ToPlainText(string markdown)
    {
        var lines = markdown.ReplaceLineEndings("\n").Split('\n')
            .Select(line => string.Concat(Split(Heading().Replace(line, string.Empty)).Select(r => r.Text)));
        return string.Join('\n', lines);
    }

    [GeneratedRegex(@"\*\*(?<bold>\S(?:.*?\S)?)\*\*|(?<![*\w])\*(?<italic>[^\s*](?:[^*]*?[^\s*])?)\*(?![*\w])")]
    private static partial Regex Emphasis();

    [GeneratedRegex(@"^\s{0,3}#{1,6}\s+")]
    private static partial Regex Heading();
}
