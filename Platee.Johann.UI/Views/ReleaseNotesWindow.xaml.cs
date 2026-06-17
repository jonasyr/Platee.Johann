namespace Platee.Johann.UI.Views;

public partial class ReleaseNotesWindow : System.Windows.Window
{
    public ReleaseNotesWindow(string html)
    {
        this.InitializeComponent();
        this.Loaded += (_, _) => this.Browser.NavigateToString(html);
    }

    private void Close_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        this.Close();
    }
}
