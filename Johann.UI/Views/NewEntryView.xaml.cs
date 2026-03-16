using Johann.UI.ViewModels;
using System.Windows;

namespace Johann.UI.Views;

public partial class NewEntryView : Window
{
    public NewEntryView(NewEntryViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Loaded += (_, _) => ProjectBox.Focus();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e) => DialogResult = true;
    private void OnCancelClick(object sender, RoutedEventArgs e) => DialogResult = false;
}
