namespace Platee.Johann.UI.Views;

using Platee.Johann.UI.ViewModels;

/// <summary>
/// Explains why ticking a template in the sidebar can appear to do nothing.
/// <para>
/// The list controls visibility, not generation. Until #65 marks ungenerated templates in
/// the entry itself, this says so once and names the workaround. It offers a permanent
/// dismissal because a hint that keeps reappearing is worse than none.
/// </para>
/// </summary>
public partial class EmptySectionHintDialog : System.Windows.Window
{
    public EmptySectionHintDialog()
    {
        this.InitializeComponent();
        this.HeadlineText.Text = EmptySectionHint.Title;
        this.BodyText.Text = EmptySectionHint.Message;
    }

    /// <summary>Gets a value indicating whether the user asked never to see this again.</summary>
    public bool Suppress => this.DoNotShowAgain.IsChecked == true;

    private void Ok_Click(object sender, System.Windows.RoutedEventArgs e)
        => this.DialogResult = true;
}
