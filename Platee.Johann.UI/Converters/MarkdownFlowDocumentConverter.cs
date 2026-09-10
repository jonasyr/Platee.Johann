namespace Platee.Johann.UI.Converters;

using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

/// <summary>
/// Converts a Markdown string to a WPF FlowDocument for display in a
/// FlowDocumentScrollViewer.  Handles: # / ## / ### headings,
/// - / * / indented bullet lists, 1. numbered lists,
/// **bold** / *italic* inline, blank-line spacing, and plain text.
/// </summary>
[ValueConversion(typeof(string), typeof(FlowDocument))]
public sealed class MarkdownFlowDocumentConverter : IValueConverter
{
    private static readonly Brush NeutralHeadingBrush = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33));
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => BuildDocument(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    // ---------------------------------------------------------------
    private static readonly Regex NumberedItemRx = new(@"^\d+\.\s+", RegexOptions.Compiled);
    private static readonly Regex InlineRx = new(@"(\*\*[^*]+\*\*|\*[^*]+\*)", RegexOptions.Compiled);

    private static FlowDocument BuildDocument(string? markdown)
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(0),
            ColumnWidth = double.PositiveInfinity,
            TextAlignment = TextAlignment.Left,
            FontFamily = new FontFamily("Segoe UI, Arial"),
            FontSize = 13,
            LineHeight = 19,
        };

        if (string.IsNullOrWhiteSpace(markdown))
        {
            return doc;
        }

        // Indentation is kept per bullet so nested lists survive. Detecting bullets on the
        // trimmed line alone used to flatten every outline into a single level.
        var bulletBuffer = new List<(int Indent, string Text)>();

        void FlushBullets()
        {
            if (bulletBuffer.Count == 0)
            {
                return;
            }

            var root = CreateList();
            doc.Blocks.Add(root);

            // One entry per open nesting level, outermost first. Indents are compared
            // relatively, so two-space and four-space markdown both nest correctly.
            var open = new List<(int Indent, List List)> { (bulletBuffer[0].Indent, root) };

            foreach (var (indent, text) in bulletBuffer)
            {
                while (open.Count > 1 && indent < open[^1].Indent)
                {
                    open.RemoveAt(open.Count - 1);
                }

                List target;
                if (indent > open[^1].Indent)
                {
                    var parent = open[^1].List;
                    var host = parent.ListItems.LastOrDefault();
                    if (host is null)
                    {
                        // Indented without a parent bullet above it — keep the text rather
                        // than dropping it.
                        host = new ListItem();
                        parent.ListItems.Add(host);
                    }

                    var child = CreateList();
                    host.Blocks.Add(child);
                    open.Add((indent, child));
                    target = child;
                }
                else
                {
                    target = open[^1].List;
                }

                var para = new Paragraph { Margin = new Thickness(0), Padding = new Thickness(0) };
                foreach (var inline in ParseInlines(text))
                {
                    para.Inlines.Add(inline);
                }

                target.ListItems.Add(new ListItem(para));
            }

            bulletBuffer.Clear();
        }

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd();
            var trimmed = line.TrimStart();   // used for prefix detection

            if (trimmed.StartsWith("### ", StringComparison.Ordinal))
            {
                FlushBullets();
                var para = new Paragraph
                {
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13.5,
                    Foreground = NeutralHeadingBrush,
                    Margin = new Thickness(0, 12, 0, 4),
                };
                foreach (var inline in ParseInlines(trimmed[4..]))
                {
                    para.Inlines.Add(inline);
                }

                doc.Blocks.Add(para);
            }
            else if (trimmed.StartsWith("## ", StringComparison.Ordinal))
            {
                FlushBullets();
                var para = new Paragraph { FontWeight = FontWeights.SemiBold, FontSize = 15, Margin = new Thickness(0, 10, 0, 2) };
                foreach (var inline in ParseInlines(trimmed[3..]))
                {
                    para.Inlines.Add(inline);
                }

                doc.Blocks.Add(para);
            }
            else if (trimmed.StartsWith("# ", StringComparison.Ordinal))
            {
                FlushBullets();
                var para = new Paragraph { FontWeight = FontWeights.Bold, FontSize = 17, Margin = new Thickness(0, 12, 0, 4) };
                foreach (var inline in ParseInlines(trimmed[2..]))
                {
                    para.Inlines.Add(inline);
                }

                doc.Blocks.Add(para);
            }
            else if (trimmed.StartsWith("- ", StringComparison.Ordinal) ||
                     trimmed.StartsWith("* ", StringComparison.Ordinal))
            {
                bulletBuffer.Add((IndentWidth(line), trimmed[2..]));
            }
            else if (NumberedItemRx.IsMatch(trimmed))
            {
                // Numbered list item — same buffer, the marker style is not critical.
                var text = NumberedItemRx.Replace(trimmed, string.Empty);
                bulletBuffer.Add((IndentWidth(line), text));
            }
            else if (string.IsNullOrEmpty(line))
            {
                FlushBullets();
                doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 2, 0, 2) });
            }
            else
            {
                FlushBullets();
                var para = CreateBodyParagraph(trimmed);
                foreach (var inline in ParseInlines(trimmed))
                {
                    para.Inlines.Add(inline);
                }

                doc.Blocks.Add(para);
            }
        }

        FlushBullets();
        return doc;
    }

    private static List CreateList() => new()
    {
        MarkerStyle = TextMarkerStyle.Disc,
        Margin = new Thickness(16, 0, 0, 4),
        Padding = new Thickness(4, 0, 0, 0),
    };

    /// <summary>
    /// Leading whitespace measured in columns, with a tab counting as two. Only relative
    /// differences matter, so the exact width never needs to match an editor's setting.
    /// </summary>
    private static int IndentWidth(string line)
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

    /// <summary>
    /// Splits a line on **bold** and *italic* markers and returns the corresponding Inlines.
    /// </summary>
    private static IEnumerable<Inline> ParseInlines(string text)
    {
        if (string.IsNullOrEmpty(text))
        {
            yield break;
        }

        var parts = InlineRx.Split(text);
        foreach (var part in parts)
        {
            if (string.IsNullOrEmpty(part))
            {
                continue;
            }

            if (part.StartsWith("**") && part.EndsWith("**") && part.Length > 4)
            {
                yield return new Bold(new Run(part[2..^2]));
            }
            else if (part.StartsWith('*') && part.EndsWith('*') && part.Length > 2)
            {
                yield return new Italic(new Run(part[1..^1]));
            }
            else
            {
                yield return new Run(part);
            }
        }
    }

    private static Paragraph CreateBodyParagraph(string text)
    {
        if (LooksLikeInlineHeading(text))
        {
            return new Paragraph
            {
                Margin = new Thickness(0, 12, 0, 4),
                FontWeight = FontWeights.SemiBold,
                FontSize = 13.5,
                Foreground = NeutralHeadingBrush,
            };
        }

        return new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
    }

    private static bool LooksLikeInlineHeading(string text)
    {
        if (!(text.StartsWith("**", StringComparison.Ordinal) &&
              text.EndsWith("**", StringComparison.Ordinal)))
        {
            return false;
        }

        var inner = text[2..^2].Trim();
        return inner.Length > 0 &&
               !inner.Contains("**", StringComparison.Ordinal) &&
               !inner.Contains('*') &&
               !inner.Contains('.') &&
               !inner.Contains(':');
    }
}
