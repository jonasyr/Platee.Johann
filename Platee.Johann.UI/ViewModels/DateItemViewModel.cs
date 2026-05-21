namespace Platee.Johann.UI.ViewModels;

using CommunityToolkit.Mvvm.ComponentModel;

public sealed partial class DateItemViewModel : ObservableObject
{
    private static readonly string[] MonthsDe =
    [
        "JANUAR", "FEBRUAR", "MÄRZ", "APRIL", "MAI", "JUNI",
        "JULI", "AUGUST", "SEPTEMBER", "OKTOBER", "NOVEMBER", "DEZEMBER"
    ];

    public DateOnly Date { get; }


    public string Key => this.Date.ToString("yyyy-MM-dd");


    public string DisplayText => this.Date.ToString("dd.MM.");


    public string MonthYearKey => $"{MonthsDe[this.Date.Month - 1]} {this.Date.Year}";

    [ObservableProperty]
    private int pendingCount;

    [ObservableProperty]
    private int totalCount;

    public bool AllDone => this.PendingCount == 0;

    public DateItemViewModel(DateOnly date) => this.Date = date;

    public void UpdateCounts(int total, int pending)
    {
        this.TotalCount = total;
        this.PendingCount = pending;
    }

    partial void OnPendingCountChanged(int value) => OnPropertyChanged(nameof(AllDone));


    partial void OnTotalCountChanged(int value) => OnPropertyChanged(nameof(AllDone));
    public override string ToString() => this.DisplayText;
}
