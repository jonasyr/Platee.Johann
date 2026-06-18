namespace Platee.Johann.UI.Views;

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Platee.Johann.UI.ViewModels;

public partial class SettingsView : Window
{
    public SettingsView(SettingsViewModel viewModel)
    {
        this.InitializeComponent();
        this.DataContext = viewModel;
    }

    private void CorrectionCorrectField_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key is not (Key.Tab or Key.Enter))
        {
            return;
        }

        if (sender is not TextBox textBox || this.DataContext is not SettingsViewModel vm)
        {
            return;
        }

        // Only auto-add when on the last row
        var currentEntry = textBox.DataContext as CorrectionEntryViewModel;
        if (currentEntry is null || vm.Korrekturen.Count == 0 || vm.Korrekturen[^1] != currentEntry)
        {
            return;
        }

        // Add a new row and move focus to its "Wrong" field on next layout pass
        vm.AddCorrectionCommand.Execute(null);

        if (e.Key == Key.Enter)
        {
            e.Handled = true;
        }

        this.Dispatcher.InvokeAsync(() =>
        {
            textBox.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
        }, System.Windows.Threading.DispatcherPriority.Input);
    }
}
