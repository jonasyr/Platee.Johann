namespace Platee.Johann.UI.Views;

using System.Windows.Controls;
using System.Windows.Input;
using Platee.Johann.UI.ViewModels;

public partial class ToastView : UserControl
{
    public ToastView()
    {
        InitializeComponent();
    }

    private void OnMouseEnter(object sender, MouseEventArgs e)
    {
        if (DataContext is ToastItem item)
            item.IsHovered = true;
    }

    private void OnMouseLeave(object sender, MouseEventArgs e)
    {
        if (DataContext is ToastItem item)
            item.IsHovered = false;
    }
}
