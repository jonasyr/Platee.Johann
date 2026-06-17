namespace Platee.Johann.UI.Views;

public partial class AdminPasswordDialog : System.Windows.Window
{
    public string EnteredPassword { get; private set; } = string.Empty;

    public AdminPasswordDialog()
    {
        this.InitializeComponent();
        this.PasswordInput.Focus();
    }

    private void Login_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        this.EnteredPassword = this.PasswordInput.Password;
        this.DialogResult = true;
    }

    private void Cancel_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        this.DialogResult = false;
    }
}
