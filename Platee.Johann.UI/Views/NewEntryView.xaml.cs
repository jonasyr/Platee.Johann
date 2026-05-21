namespace Platee.Johann.UI.Views;

using System.Windows;
using Platee.Johann.UI.ViewModels;

public partial class NewEntryView : Window
{
    public NewEntryView(NewEntryViewModel viewModel)
    {
        this.InitializeComponent();
        this.DataContext = viewModel;
        this.Loaded += (_, _) => this.ProjectBox.Focus();
    }

    private void OnSaveClick(object sender, RoutedEventArgs e)
    {
        if (this.DataContext is NewEntryViewModel vm && vm.DialogResult)
        {
            this.DialogResult = true;
        }
    }

    private void OnCancelClick(object sender, RoutedEventArgs e) => this.DialogResult = false;
}
