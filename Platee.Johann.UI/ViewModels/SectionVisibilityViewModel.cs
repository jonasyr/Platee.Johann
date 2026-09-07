namespace Platee.Johann.UI.ViewModels;

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
