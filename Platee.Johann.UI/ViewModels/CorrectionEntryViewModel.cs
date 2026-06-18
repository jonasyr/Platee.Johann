using CommunityToolkit.Mvvm.ComponentModel;

namespace Platee.Johann.UI.ViewModels;

public sealed partial class CorrectionEntryViewModel : ObservableObject
{
    [ObservableProperty]
    private string wrong = string.Empty;

    [ObservableProperty]
    private string correct = string.Empty;
}
