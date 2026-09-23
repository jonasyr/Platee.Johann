namespace Platee.Johann.UI;

using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Platee.Johann.UI.Helpers;
using Platee.Johann.UI.ViewModels;

public partial class MainWindow : Window
{
    public string AppVersion { get; } =
        "v" + (Assembly.GetExecutingAssembly().GetName().Version?.ToString(3) ?? "?");

    private readonly MainViewModel viewModel;
    private Point dragStartPoint;
    private bool isDragging;

    public MainWindow(MainViewModel viewModel)
    {
        this.InitializeComponent();
        this.viewModel = viewModel;
        this.DataContext = viewModel;
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await this.viewModel.InitializeAsync();
    }

    /// <summary>
    /// Intercepts mouse-wheel events before nested FlowDocumentScrollViewers can absorb them,
    /// ensuring the outer detail ScrollViewer always scrolls.
    /// </summary>
    private void EntryListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        this.dragStartPoint = e.GetPosition(null);
    }

    private async void EntryListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed)
        {
            return;
        }

        if (this.isDragging)
        {
            return;
        }

        var diff = e.GetPosition(null) - this.dragStartPoint;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance)
        {
            return;
        }

        var row = FindAncestorRowViewModel(e.OriginalSource as DependencyObject);
        if (row is null)
        {
            return;
        }

        this.isDragging = true;
        try
        {
            var filePath = await this.viewModel.Detail.RenderPdfForDragAsync(
                row.Entry, CancellationToken.None);

            if (filePath is null)
            {
                return;
            }

            if (Mouse.LeftButton != MouseButtonState.Pressed)
            {
                return; // released during render
            }

            var data = new DataObject(DataFormats.FileDrop, new[] { filePath });
            DragDrop.DoDragDrop(this.EntryListBox, data, DragDropEffects.Copy);
        }
        finally
        {
            this.isDragging = false;
        }
    }

    private static EntryRowViewModel? FindAncestorRowViewModel(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is FrameworkElement { DataContext: EntryRowViewModel row })
            {
                return row;
            }

            // Run / Span are FrameworkContentElements — not Visuals — so use the logical
            // parent to escape them; once back in the visual tree switch to VisualTreeHelper.
            source = source is Visual or Visual3D
                ? VisualTreeHelper.GetParent(source)
                : LogicalTreeHelper.GetParent(source);
        }

        return null;
    }

    private void DetailScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (Keyboard.Modifiers == ModifierKeys.Control)
        {
            if (e.Delta > 0)
            {
                this.viewModel.Detail?.ZoomInCommand.Execute(null);
            }
            else
            {
                this.viewModel.Detail?.ZoomOutCommand.Execute(null);
            }

            e.Handled = true;
            return;
        }

        if (sender is ScrollViewer sv)
        {
            sv.ScrollToVerticalOffset(sv.VerticalOffset - (e.Delta / 3.0));
            e.Handled = true;
        }
    }

    // ── Spaltenbreite an den Inhalt anpassen (Doppelklick auf die Trennlinie, #96) ──────────

    /// <summary>What the detail view keeps when a list column is widened to fit its content.</summary>
    private const double DetailMinWidth = 360;

    private void DateSplitter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        this.FitColumnToContent(this.DateColumn, this.DatePane, this.DateListBox);
        e.Handled = true;
    }

    private void EntrySplitter_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        this.FitColumnToContent(this.EntryColumn, this.EntryPane, this.EntryListBox);
        e.Handled = true;
    }

    /// <summary>
    /// Like double-clicking a column border in Excel: the column takes the width of its widest
    /// content — every list row (not only the realised ones; the list virtualises), the group
    /// headers, and the rest of the pane (header bar, the "Im Eintrag anzeigen" ticks). The
    /// space comes from the detail view, which keeps <see cref="DetailMinWidth"/>.
    /// </summary>
    private void FitColumnToContent(ColumnDefinition column, Panel pane, ListBox list)
    {
        var rows = list.Items.Cast<object>().Select(item => MeasureRow(list, item))
            .Concat(MeasureGroupHeaders(list))
            .DefaultIfEmpty(0)
            .Max();
        var rest = pane.Children.OfType<FrameworkElement>()
            .Where(child => !ReferenceEquals(child, list) && child.Visibility == Visibility.Visible)
            .Select(MeasureUnconstrained)
            .DefaultIfEmpty(0)
            .Max();

        // Measuring live elements unconstrained leaves them with the wrong desired size until
        // the pane is measured again.
        pane.InvalidateMeasure();

        var widest = Math.Max(rows + ListChrome(list), rest);
        var max = column.ActualWidth + this.DetailColumn.ActualWidth - DetailMinWidth;
        var width = ColumnAutoFit.Width(widest, chrome: 0, column.MinWidth, max);
        if (width is not null)
        {
            column.Width = new GridLength(width.Value);
        }
    }

    /// <summary>
    /// Builds a detached row with the list's own container style and template and measures it
    /// untrimmed. Text properties are copied, since a detached element inherits nothing.
    /// </summary>
    private static double MeasureRow(ListBox list, object item)
    {
        var row = new ListBoxItem
        {
            Style = list.ItemContainerStyle,
            Content = item,
            ContentTemplate = list.ItemTemplate,
            DataContext = item,
        };
        CopyTextProperties(list, row);
        return MeasureUnconstrained(row);
    }

    private static IEnumerable<double> MeasureGroupHeaders(ListBox list)
    {
        var template = list.GroupStyle.FirstOrDefault()?.HeaderTemplate;
        if (template is null || list.Items.Groups is null)
        {
            yield break;
        }

        foreach (var group in list.Items.Groups)
        {
            var header = new ContentControl { Content = group, ContentTemplate = template };
            CopyTextProperties(list, header);
            yield return MeasureUnconstrained(header);
        }
    }

    private static double MeasureUnconstrained(FrameworkElement element)
    {
        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return element.DesiredSize.Width + element.Margin.Left + element.Margin.Right;
    }

    private static void CopyTextProperties(Control source, Control target)
    {
        target.FontFamily = source.FontFamily;
        target.FontSize = source.FontSize;
        target.FontStyle = source.FontStyle;
        target.FontWeight = source.FontWeight;
        target.FontStretch = source.FontStretch;
    }

    /// <summary>The list's own border and padding, plus its vertical scroll bar when shown.</summary>
    private static double ListChrome(ListBox list)
    {
        var chrome = list.BorderThickness.Left + list.BorderThickness.Right
                     + list.Padding.Left + list.Padding.Right;
        var scrollViewer = FindDescendant<ScrollViewer>(list);
        if (scrollViewer?.ComputedVerticalScrollBarVisibility == Visibility.Visible)
        {
            chrome += SystemParameters.VerticalScrollBarWidth;
        }

        return chrome;
    }

    private static T? FindDescendant<T>(DependencyObject parent)
        where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);
            if (child is T match)
            {
                return match;
            }

            if (FindDescendant<T>(child) is { } nested)
            {
                return nested;
            }
        }

        return null;
    }
}
