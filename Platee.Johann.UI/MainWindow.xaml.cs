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
        viewModel.ShowReleaseNotes = this.ShowReleaseNotes;
    }

    /// <summary>
    /// Shows the release notes — after an update at startup, and from the „Neuigkeiten“ button
    /// (#78). Afterwards the button pulses briefly, so the user sees where to find them again.
    /// </summary>
    public void ShowReleaseNotes()
    {
        var markdown = ReleaseNotesHelper.LoadMarkdown(typeof(App).Assembly);
        if (string.IsNullOrWhiteSpace(markdown))
        {
            MessageBox.Show(
                this,
                "Die Neuigkeiten konnten nicht geladen werden.",
                "Neuigkeiten",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        new Views.ReleaseNotesWindow(ReleaseNotesHelper.RenderToHtml(markdown)) { Owner = this }.ShowDialog();
        this.PulseReleaseNotesButton();
    }

    /// <summary>
    /// Scales the button up and back twice. The simple variant from #78: a window cannot be
    /// animated into an element of another window, and a pulse serves the same purpose.
    /// Skipped when Windows animations are switched off.
    /// </summary>
    private void PulseReleaseNotesButton()
    {
        if (!SystemParameters.ClientAreaAnimation || this.ReleaseNotesButton.RenderTransform is not ScaleTransform scale)
        {
            return;
        }

        var pulse = new System.Windows.Media.Animation.DoubleAnimation(1.0, 1.2, TimeSpan.FromMilliseconds(220))
        {
            AutoReverse = true,
            RepeatBehavior = new System.Windows.Media.Animation.RepeatBehavior(2),
        };
        scale.BeginAnimation(ScaleTransform.ScaleXProperty, pulse);
        scale.BeginAnimation(ScaleTransform.ScaleYProperty, pulse);
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
    /// content — every list row, the group
    /// headers, and the rest of the pane (header bar, the "Im Eintrag anzeigen" ticks). The
    /// space comes from the detail view, which keeps <see cref="DetailMinWidth"/>.
    /// </summary>
    private void FitColumnToContent(ColumnDefinition column, Panel pane, ListBox list)
    {
        // The rows are measured where they live — with their bindings, fonts and styles. A
        // detached copy of a row measured without its bound title and came out far too narrow.
        // All rows exist: the entry list does not virtualise, and a grouped list never does.
        var presenter = FindDescendant<ItemsPresenter>(list);

        // Everything between the column edge and the rows, measured rather than added up: the
        // ListBox template pads its rows by a fixed 1 px that no property shows, and those 2 px
        // were enough to trim the title again (#96). A visible scroll bar is included as well.
        var chrome = presenter is null ? 0 : column.ActualWidth - presenter.ActualWidth;
        var rows = presenter is null ? 0 : MeasureUnconstrained(presenter);
        var rest = pane.Children.OfType<FrameworkElement>()
            .Where(child => !ReferenceEquals(child, list) && child.Visibility == Visibility.Visible)
            .Select(MeasureUnconstrained)
            .DefaultIfEmpty(0)
            .Max();

        // Measuring live elements unconstrained leaves them with the wrong desired size until
        // they are measured again.
        list.InvalidateMeasure();
        pane.InvalidateMeasure();

        var widest = Math.Max(rows > 0 ? rows + chrome : 0, rest);
        var max = column.ActualWidth + this.DetailColumn.ActualWidth - DetailMinWidth;
        var width = ColumnAutoFit.Width(widest, chrome: 0, column.MinWidth, max);
        if (width is not null)
        {
            column.Width = new GridLength(width.Value);
        }
    }

    private static double MeasureUnconstrained(FrameworkElement element)
    {
        element.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
        return element.DesiredSize.Width + element.Margin.Left + element.Margin.Right;
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
