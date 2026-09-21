namespace Platee.Johann.Domain.Services;

/// <summary>One markdown bullet: leading whitespace in columns and the text after the marker.</summary>
public readonly record struct BulletLine(int Indent, string Text);

/// <summary>
/// Recognises markdown bullets and assigns nesting levels from their indentation.
/// <para>
/// Same rule as <c>MarkdownFlowDocumentConverter</c> in the detail view: indents are compared
/// relatively, so two-space and four-space markdown nest alike, a deeper indent opens at most one
/// level, and an indented bullet without a parent stays on the outermost level instead of being
/// lost. The PDF used to recognise bullets only without indentation and printed every sub-point as
/// a paragraph with a literal hyphen (#83).
/// </para>
/// </summary>
public static class BulletOutline
{
    private static readonly string[] CheckboxPrefixes = ["[ ] ", "[x] ", "[X] "];

    /// <summary>
    /// Parses <c>- </c>, <c>* </c> or <c>+ </c> after optional indentation. A leading task-list
    /// checkbox is dropped: its brackets are syntax, not text.
    /// </summary>
    public static bool TryParse(string line, out BulletLine bullet)
    {
        bullet = default;
        var trimmed = line.TrimStart();
        if (trimmed.Length < 2 || trimmed[1] != ' ' || trimmed[0] is not ('-' or '*' or '+'))
        {
            return false;
        }

        var text = trimmed[2..].TrimEnd();
        foreach (var prefix in CheckboxPrefixes)
        {
            if (text.StartsWith(prefix, StringComparison.Ordinal))
            {
                text = text[prefix.Length..];
                break;
            }
        }

        bullet = new BulletLine(IndentWidth(line), text);
        return true;
    }

    /// <summary>Nesting level (0 = outermost) for each indent of one contiguous bullet block.</summary>
    public static IReadOnlyList<int> AssignLevels(IReadOnlyList<int> indents)
    {
        var levels = new int[indents.Count];
        if (indents.Count == 0)
        {
            return levels;
        }

        // One entry per open level, outermost first; the first bullet defines level 0.
        var open = new List<int> { indents[0] };
        for (var i = 0; i < indents.Count; i++)
        {
            var indent = indents[i];
            while (open.Count > 1 && indent < open[^1])
            {
                open.RemoveAt(open.Count - 1);
            }

            // Shallower than the first bullet (which was an indented orphan): this one is the
            // real outermost level, so later indents are measured from here.
            if (indent < open[0])
            {
                open[0] = indent;
            }

            if (indent > open[^1])
            {
                open.Add(indent);
            }

            levels[i] = open.Count - 1;
        }

        return levels;
    }

    /// <summary>Leading whitespace in columns, a tab counting as two; only differences matter.</summary>
    public static int IndentWidth(string line)
    {
        var width = 0;
        foreach (var ch in line)
        {
            if (ch == ' ')
            {
                width++;
            }
            else if (ch == '\t')
            {
                width += 2;
            }
            else
            {
                break;
            }
        }

        return width;
    }
}
