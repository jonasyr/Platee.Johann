using CommunityToolkit.Mvvm.ComponentModel;

namespace Platee.Johann.UI.ViewModels;

public sealed partial class DateItemViewModel : ObservableObject
{
    private static readonly string[] MonthsDe =
    [
        "JANUAR", "FEBRUAR", "MÄRZ", "APRIL", "MAI", "JUNI",
        "JULI", "AUGUST", "SEPTEMBER", "OKTOBER", "NOVEMBER", "DEZEMBER"
    ];

    public DateOnly Date { get; }
    public string Key => Date.ToString("yyyy-MM-dd");
    public string DisplayText => Date.ToString("dd.MM.");
    public string MonthYearKey => $"{MonthsDe[Date.Month - 1]} {Date.Year}";

    [ObservableProperty]
    private int _pendingCount;

    [ObservableProperty]
    private int _totalCount;

    public bool AllDone => PendingCount == 0;

    public DateItemViewModel(DateOnly date) => Date = date;

    public void UpdateCounts(int total, int pending)
    {
        TotalCount = total;
        PendingCount = pending;
    }

    partial void OnPendingCountChanged(int value) => OnPropertyChanged(nameof(AllDone));
    partial void OnTotalCountChanged(int value) => OnPropertyChanged(nameof(AllDone));

    public override string ToString() => DisplayText;
}
