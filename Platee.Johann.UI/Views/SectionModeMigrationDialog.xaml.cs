namespace Platee.Johann.UI.Views;

/// <summary>
/// The one-time v1.4.0 first-run prompt for per-section generation modes.
/// <para>
/// Dropping from eight generated sections to four is a visible behaviour change nobody
/// asked for, so it is offered rather than applied silently. Closing the window without
/// choosing leaves <see cref="UseRecommended"/> null and persists nothing, so the prompt
/// reappears at the next start instead of silently deciding for the user.
/// </para>
/// </summary>
public partial class SectionModeMigrationDialog : System.Windows.Window
{
    public SectionModeMigrationDialog()
    {
        this.InitializeComponent();
    }

    /// <summary>
    /// Gets the chosen preset: true for the recommended four-auto preset, false for the
    /// previous always-generate-everything behaviour, null when the user closed the dialog.
    /// </summary>
    public bool? UseRecommended { get; private set; }

    private void Recommended_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        this.UseRecommended = true;
        this.DialogResult = true;
    }

    private void KeepAll_Click(object sender, System.Windows.RoutedEventArgs e)
    {
        this.UseRecommended = false;
        this.DialogResult = true;
    }
}
