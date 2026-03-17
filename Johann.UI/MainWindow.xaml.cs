using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using Johann.UI.ViewModels;

namespace Johann.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;
    private Point _dragStartPoint;
    private bool  _isDragging;

    public MainWindow(MainViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        DataContext = viewModel;
    }

    protected override async void OnContentRendered(EventArgs e)
    {
        base.OnContentRendered(e);
        await _viewModel.InitializeAsync();
    }

    /// <summary>
    /// Intercepts mouse-wheel events before nested FlowDocumentScrollViewers can absorb them,
    /// ensuring the outer detail ScrollViewer always scrolls.
    /// </summary>
    private void EntryListBox_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        _dragStartPoint = e.GetPosition(null);
    }

    private async void EntryListBox_PreviewMouseMove(object sender, MouseEventArgs e)
    {
        if (e.LeftButton != MouseButtonState.Pressed) return;
        if (_isDragging) return;

        var diff = e.GetPosition(null) - _dragStartPoint;
        if (Math.Abs(diff.X) < SystemParameters.MinimumHorizontalDragDistance &&
            Math.Abs(diff.Y) < SystemParameters.MinimumVerticalDragDistance) return;

        var row = FindAncestorRowViewModel(e.OriginalSource as DependencyObject);
        if (row is null) return;

        _isDragging = true;
        try
        {
            var filePath = await _viewModel.Detail.RenderPdfForDragAsync(
                row.Entry, CancellationToken.None);

            if (filePath is null) return;
            if (Mouse.LeftButton != MouseButtonState.Pressed) return; // released during render

            var data = new DataObject(DataFormats.FileDrop, new[] { filePath });
            DragDrop.DoDragDrop(EntryListBox, data, DragDropEffects.Copy);
        }
        finally
        {
            _isDragging = false;
        }
    }

    private static EntryRowViewModel? FindAncestorRowViewModel(DependencyObject? source)
    {
        while (source is not null)
        {
            if (source is FrameworkElement { DataContext: EntryRowViewModel row })
                return row;
            source = VisualTreeHelper.GetParent(source);
        }
        return null;
    }

    private void DetailScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }
}
