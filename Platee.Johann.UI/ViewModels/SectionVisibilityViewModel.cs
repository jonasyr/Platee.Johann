namespace Platee.Johann.UI.ViewModels;

using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;

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

    /// <summary>Group heading for a user's own categories.</summary>
    public const string PersonalGroup = "Eigene Kategorien";

    /// <summary>Group heading for categories from the shared team file.</summary>
    public const string GlobalGroup = "Team-Kategorien";

    /// <summary>Group heading for text whose category has been deleted.</summary>
    public const string OrphanGroup = "Nicht mehr konfiguriert";

    /// <summary>
    /// Rebuilds the custom-category checkboxes from the current catalog and the selected
    /// entry, keeping whatever the user had already ticked.
    /// <para>
    /// The entry matters because text whose category has been deleted still renders, in the
    /// detail view and in every export. Without a toggle of its own it could not be hidden
    /// anywhere at all, which made a deleted category permanently louder than a live one.
    /// </para>
    /// </summary>
    public void SyncCustomSections(
        IEnumerable<SectionDescriptor> catalog,
        IReadOnlyDictionary<string, string>? entrySections = null,
        IReadOnlyDictionary<string, string>? entrySectionNames = null)
    {
        var wanted = new List<(string Id, string Name, string Group)>();
        var seen = new HashSet<string>(StringComparer.Ordinal);

        foreach (var descriptor in catalog.Where(d => !d.IsBuiltIn))
        {
            var group = descriptor.Category?.Scope == CategoryScope.Global ? GlobalGroup : PersonalGroup;
            wanted.Add((descriptor.Id, descriptor.Name, group));
            seen.Add(descriptor.Id);
        }

        foreach (var id in (entrySections?.Keys ?? []).OrderBy(k => k, StringComparer.Ordinal))
        {
            if (seen.Contains(id) || string.IsNullOrWhiteSpace(entrySections![id]))
            {
                continue;
            }

            var name = entrySectionNames is not null
                       && entrySectionNames.TryGetValue(id, out var recorded)
                       && !string.IsNullOrWhiteSpace(recorded)
                ? recorded
                : id;
            wanted.Add((id, name, OrphanGroup));
        }

        var current = this.CustomSections
            .Select(t => (t.Id, t.Name, t.Group))
            .ToList();
        if (current.SequenceEqual(wanted))
        {
            return;
        }

        var previous = this.CustomSections.ToDictionary(t => t.Id, t => t.IsVisible, StringComparer.Ordinal);
        this.CustomSections.Clear();
        this.CustomSectionVisibility.Clear();

        foreach (var (id, name, group) in wanted)
        {
            this.CustomSections.Add(new CustomSectionToggleViewModel(
                id,
                name,
                group,
                previous.TryGetValue(id, out var wasVisible) ? wasVisible : true,
                this.SetCustomVisibility));
        }

        this.OnPropertyChanged(nameof(this.CustomSectionVisibility));
    }

    private void SetCustomVisibility(string id, bool isVisible)
    {
        this.CustomSectionVisibility[id] = isVisible;

        // The dictionary itself raises nothing; the detail view listens for this to
        // re-evaluate which sections it renders.
        this.OnPropertyChanged(nameof(this.CustomSectionVisibility));
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
