using System.Windows;
using Platee.Johann.UI.ViewModels;

namespace Platee.Johann.UI.Views;

public partial class SettingsView : Window
{
    public SettingsView(SettingsViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }
}
