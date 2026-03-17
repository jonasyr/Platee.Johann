using CommunityToolkit.Mvvm.ComponentModel;
using Johann.Application.Processing;

namespace Johann.UI.ViewModels;

public sealed partial class SectionVisibilityViewModel : ObservableObject
{
    [ObservableProperty] private bool _showLongSummary       = true;
    [ObservableProperty] private bool _showProseSummary      = true;
    [ObservableProperty] private bool _showTaskList          = true;
    [ObservableProperty] private bool _showConversationNote  = true;
    [ObservableProperty] private bool _showEmailText         = false;
    [ObservableProperty] private bool _showStundenzettelText = false;
    [ObservableProperty] private bool _showAnalogText        = false;
    [ObservableProperty] private bool _showTranscript        = true;

    public SectionVisibility ToSectionVisibility() => new(
        ShowLongSummary,
        ShowProseSummary,
        ShowTaskList,
        ShowConversationNote,
        ShowEmailText,
        ShowStundenzettelText,
        ShowAnalogText,
        ShowTranscript);
}
