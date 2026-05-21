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
