namespace Platee.Johann.UI.Views;

using System.Windows;
using Platee.Johann.UI.ViewModels;

public partial class SettingsView : Window
{
    public SettingsView(SettingsViewModel viewModel)
    {
        this.InitializeComponent();
        this.DataContext = viewModel;
    }
}
