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

        var bulletBuffer = new List<string>();

        void FlushBullets()
        {
            if (bulletBuffer.Count == 0)
            {
                return;
            }

            var list = new List
            {
                MarkerStyle = TextMarkerStyle.Disc,
                Margin = new Thickness(16, 0, 0, 4),
                Padding = new Thickness(4, 0, 0, 0),
            };
            foreach (var item in bulletBuffer)
            {
                var para = new Paragraph { Margin = new Thickness(0), Padding = new Thickness(0) };
                foreach (var inline in ParseInlines(item))
                {
                    para.Inlines.Add(inline);
                }

                list.ListItems.Add(new ListItem(para));
            }

            doc.Blocks.Add(list);
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
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                    Margin = new Thickness(0, 8, 0, 2),
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
                bulletBuffer.Add(trimmed[2..]);
            }
            else if (NumberedItemRx.IsMatch(trimmed))
            {
                // Numbered list item — add to same bullet buffer (visual distinction not critical)
                var text = NumberedItemRx.Replace(trimmed, string.Empty);
                bulletBuffer.Add(text);
            }
            else if (string.IsNullOrEmpty(line))
            {
                FlushBullets();
                doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 2, 0, 2) });
            }
            else
            {
                FlushBullets();
                var para = new Paragraph { Margin = new Thickness(0, 0, 0, 2) };
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
}
