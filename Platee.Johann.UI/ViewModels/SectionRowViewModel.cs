namespace Platee.Johann.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

/// <summary>
/// One row in the custom-section list of the detail view.
/// <para>
/// An orphaned row is text whose category has since been deleted. It is kept and shown
/// read-only under „Nicht mehr konfiguriert“ rather than silently dropped — the generated
/// text is still in the entry file and the user may still want it (spec risk 9).
/// </para>
/// </summary>
public sealed partial class SectionRowViewModel : ObservableObject
{
    public SectionRowViewModel(string id, string name, string? text, bool isConfigured)
    {
        this.Id = id;
        this.Name = name;
        this.text = text;
        this.IsConfigured = isConfigured;
    }

    /// <summary>Gets the stable category id the text is stored under.</summary>
    public string Id { get; }

    /// <summary>Gets the display name shown as the section heading.</summary>
    public string Name { get; }

    /// <summary>Gets a value indicating whether the category still exists in the catalog.</summary>
    public bool IsConfigured { get; }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGenerated))]
    [NotifyPropertyChangedFor(nameof(IsOrphaned))]
    [NotifyPropertyChangedFor(nameof(CanGenerate))]
    private string? text;

    [ObservableProperty]
    private bool isBusy;

    /// <summary>Gets or sets a value indicating whether the section is ticked in the sidebar.</summary>
    [ObservableProperty]
    private bool isVisible = true;

    /// <summary>Gets a value indicating whether the row has generated text to show.</summary>
    public bool IsGenerated => !string.IsNullOrWhiteSpace(this.Text);

    /// <summary>Gets a value indicating whether the „Generieren“ affordance is shown.</summary>
    public bool CanGenerate => !this.IsGenerated && this.IsConfigured;

    /// <summary>Gets a value indicating whether this is text of a deleted category.</summary>
    public bool IsOrphaned => this.IsGenerated && !this.IsConfigured;
}
