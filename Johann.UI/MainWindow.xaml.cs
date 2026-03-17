using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Johann.UI.ViewModels;

namespace Johann.UI;

public partial class MainWindow : Window
{
    private readonly MainViewModel _viewModel;

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
    private void DetailScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
    {
        if (sender is ScrollViewer sv)
        {
            sv.ScrollToVerticalOffset(sv.VerticalOffset - e.Delta / 3.0);
            e.Handled = true;
        }
    }
}
