namespace Platee.Johann.UI.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Platee.Johann.Application.Processing;

public sealed partial class SectionVisibilityViewModel : ObservableObject
{
    [ObservableProperty]
    private bool showLongSummary = true;
    [ObservableProperty]
    private bool showProseSummary = true;
    [ObservableProperty]
    private bool showTaskList = true;
    [ObservableProperty]
    private bool showConversationNote = true;
    [ObservableProperty]
    private bool showEmailText = false;
    [ObservableProperty]
    private bool showStundenzettelText = false;
    [ObservableProperty]
    private bool showAnalogText = false;
    [ObservableProperty]
    private bool showTranscript = true;

    /// <summary>
    /// Gets the export visibility for user-defined sections, keyed by category id.
    /// A missing key means visible.
    /// </summary>
    /// <remarks>
    /// Deliberately a separate dictionary rather than extra fields on the positional
    /// <see cref="SectionVisibility"/> record: all three renderers consume that record's
    /// shape, and reshaping it would multiply the diff for no gain (spec risk 7).
    /// </remarks>
    public Dictionary<string, bool> CustomSectionVisibility { get; } = [];

    /// <summary>Gets one checkbox per user-defined category, in catalog order.</summary>
    public ObservableCollection<CustomSectionToggleViewModel> CustomSections { get; } = [];

    /// <summary>
    /// Rebuilds the custom-category checkboxes from the current catalog, keeping whatever
    /// the user had already ticked. Categories that disappeared drop out of the map too, so
    /// a deleted category cannot keep hiding an orphaned section forever.
    /// </summary>
    public void SyncCustomSections(IEnumerable<SectionDescriptor> catalog)
    {
        var custom = catalog.Where(d => !d.IsBuiltIn).ToList();
        if (this.CustomSections.Select(t => t.Id).SequenceEqual(custom.Select(d => d.Id), StringComparer.Ordinal)
            && this.CustomSections.Select(t => t.Name).SequenceEqual(custom.Select(d => d.Name), StringComparer.Ordinal))
        {
            return;
        }

        var previous = this.CustomSections.ToDictionary(t => t.Id, t => t.IsVisible, StringComparer.Ordinal);
        this.CustomSections.Clear();
        this.CustomSectionVisibility.Clear();

        foreach (var descriptor in custom)
        {
            this.CustomSections.Add(new CustomSectionToggleViewModel(
                descriptor.Id,
                descriptor.Name,
                this.CustomSectionVisibility,
                previous.TryGetValue(descriptor.Id, out var wasVisible) ? wasVisible : true));
        }
    }

    public SectionVisibility ToSectionVisibility() => new(
        this.ShowLongSummary,
        this.ShowProseSummary,
        this.ShowTaskList,
        this.ShowConversationNote,
        this.ShowEmailText,
        this.ShowStundenzettelText,
        this.ShowAnalogText,
        this.ShowTranscript);
}
