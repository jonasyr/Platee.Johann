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

    /// <summary>Gets the user's own categories.</summary>
    public ObservableCollection<CustomSectionToggleViewModel> PersonalSections { get; } = [];

    /// <summary>Gets the categories that come from the shared team file.</summary>
    public ObservableCollection<CustomSectionToggleViewModel> GlobalSections { get; } = [];

    /// <summary>Gets text whose category has been deleted.</summary>
    public ObservableCollection<CustomSectionToggleViewModel> OrphanedSections { get; } = [];

    public bool HasPersonalSections => this.PersonalSections.Count > 0;

    public bool HasGlobalSections => this.GlobalSections.Count > 0;

    public bool HasOrphanedSections => this.OrphanedSections.Count > 0;

    /// <summary>Gets the orphan header, which carries the count because the group starts closed.</summary>
    public string OrphanedHeader => $"{OrphanGroup.ToUpperInvariant()} ({this.OrphanedSections.Count})";

    [ObservableProperty]
    private bool isPersonalExpanded = true;

    [ObservableProperty]
    private bool isGlobalExpanded = true;

    /// <summary>
    /// Orphaned text starts collapsed: it is the least interesting group and would otherwise
    /// grow without bound as categories come and go.
    /// </summary>
    [ObservableProperty]
    private bool isOrphanedExpanded;

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

        this.PersonalSections.Clear();
        this.GlobalSections.Clear();
        this.OrphanedSections.Clear();

        foreach (var (id, name, group) in wanted)
        {
            var toggle = new CustomSectionToggleViewModel(
                id,
                name,
                group,
                previous.TryGetValue(id, out var wasVisible) ? wasVisible : true,
                this.SetCustomVisibility);

            this.CustomSections.Add(toggle);
            this.GroupFor(group).Add(toggle);
        }

        this.OnPropertyChanged(nameof(this.CustomSectionVisibility));
        this.OnPropertyChanged(nameof(this.HasPersonalSections));
        this.OnPropertyChanged(nameof(this.HasGlobalSections));
        this.OnPropertyChanged(nameof(this.HasOrphanedSections));
        this.OnPropertyChanged(nameof(this.OrphanedHeader));
    }

    private ObservableCollection<CustomSectionToggleViewModel> GroupFor(string group) => group switch
    {
        GlobalGroup => this.GlobalSections,
        OrphanGroup => this.OrphanedSections,
        _ => this.PersonalSections,
    };

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
