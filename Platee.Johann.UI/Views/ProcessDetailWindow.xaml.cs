using System.Collections.ObjectModel;
using System.Windows;
using Platee.Johann.UI.ViewModels;

namespace Platee.Johann.UI.Views;

public partial class ProcessDetailWindow : Window
{
    private readonly ObservableCollection<ProcessLogItem> _log;

    public ProcessDetailWindow(ObservableCollection<ProcessLogItem> log)
    {
        InitializeComponent();
        _log = log;
        DataContext = log;
    }

    private void ClearAll_Click(object sender, RoutedEventArgs e)
        => _log.Clear();

    private void RemoveCompleted_Click(object sender, RoutedEventArgs e)
    {
        var done = _log.Where(x => !x.IsRunning).ToList();
        foreach (var item in done)
            _log.Remove(item);
    }

    private void Close_Click(object sender, RoutedEventArgs e)
        => Close();
}
