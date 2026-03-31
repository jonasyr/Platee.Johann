using CommunityToolkit.Mvvm.ComponentModel;

namespace Platee.Johann.UI.ViewModels;

public sealed partial class DateItemViewModel : ObservableObject
{
    public DateOnly Date { get; }

    [ObservableProperty]
    private int _pendingCount;

    public string DisplayText => PendingCount > 0
        ? $"{Date:dd.MM.yy} ({PendingCount})"
        : Date.ToString("dd.MM.yy");

    public DateItemViewModel(DateOnly date) => Date = date;

    partial void OnPendingCountChanged(int value) => OnPropertyChanged(nameof(DisplayText));

    public override string ToString() => DisplayText;
}
