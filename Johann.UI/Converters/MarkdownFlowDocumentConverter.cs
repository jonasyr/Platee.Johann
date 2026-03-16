using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Media;

namespace Johann.UI.Converters;

/// <summary>
/// Converts a Markdown string to a WPF FlowDocument for display in a
/// FlowDocumentScrollViewer.  Handles: # / ## / ###  headings,
/// - / * bullet lists, blank-line spacing, and plain text.
/// </summary>
[ValueConversion(typeof(string), typeof(FlowDocument))]
public sealed class MarkdownFlowDocumentConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        => BuildDocument(value as string);

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        => throw new NotSupportedException();

    // ---------------------------------------------------------------

    private static FlowDocument BuildDocument(string? markdown)
    {
        var doc = new FlowDocument
        {
            PagePadding = new Thickness(0),
            ColumnWidth = double.PositiveInfinity,   // no multi-column layout
            TextAlignment = TextAlignment.Left,
            FontFamily = new FontFamily("Segoe UI, Arial"),
            FontSize = 13,
            LineHeight = 19,
        };

        if (string.IsNullOrWhiteSpace(markdown))
            return doc;

        var bulletBuffer = new List<string>();

        void FlushBullets()
        {
            if (bulletBuffer.Count == 0) return;
            var list = new List
            {
                MarkerStyle = TextMarkerStyle.Disc,
                Margin = new Thickness(16, 0, 0, 4),
                Padding = new Thickness(4, 0, 0, 0),
            };
            foreach (var item in bulletBuffer)
                list.ListItems.Add(new ListItem(
                    new Paragraph(new Run(item)) { Margin = new Thickness(0), Padding = new Thickness(0) }));
            doc.Blocks.Add(list);
            bulletBuffer.Clear();
        }

        foreach (var rawLine in markdown.Split('\n'))
        {
            var line = rawLine.TrimEnd();

            if (line.StartsWith("### ", StringComparison.Ordinal))
            {
                FlushBullets();
                doc.Blocks.Add(new Paragraph(new Run(line[4..]))
                {
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 13,
                    Foreground = new SolidColorBrush(Color.FromRgb(0x33, 0x33, 0x33)),
                    Margin = new Thickness(0, 8, 0, 2),
                });
            }
            else if (line.StartsWith("## ", StringComparison.Ordinal))
            {
                FlushBullets();
                doc.Blocks.Add(new Paragraph(new Run(line[3..]))
                {
                    FontWeight = FontWeights.SemiBold,
                    FontSize = 15,
                    Margin = new Thickness(0, 10, 0, 2),
                });
            }
            else if (line.StartsWith("# ", StringComparison.Ordinal))
            {
                FlushBullets();
                doc.Blocks.Add(new Paragraph(new Run(line[2..]))
                {
                    FontWeight = FontWeights.Bold,
                    FontSize = 17,
                    Margin = new Thickness(0, 12, 0, 4),
                });
            }
            else if (line.StartsWith("- ", StringComparison.Ordinal) ||
                     line.StartsWith("* ", StringComparison.Ordinal))
            {
                bulletBuffer.Add(line[2..]);
            }
            else if (string.IsNullOrEmpty(line))
            {
                FlushBullets();
                // Minimal spacer so consecutive sections breathe
                doc.Blocks.Add(new Paragraph { Margin = new Thickness(0, 2, 0, 2) });
            }
            else
            {
                FlushBullets();
                doc.Blocks.Add(new Paragraph(new Run(line))
                {
                    Margin = new Thickness(0, 0, 0, 2),
                });
            }
        }

        FlushBullets();
        return doc;
    }
}
