using System.Windows;
using Johann.UI.ViewModels;

namespace Johann.UI.Views;

public partial class SettingsView : Window
{
    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
