namespace Platee.Johann.UI.ViewModels;

public sealed class DateItemViewModel
{
    public DateOnly Date { get; }
    public string DisplayText => Date.ToString("dd.MM.yy");

    public DateItemViewModel(DateOnly date) => Date = date;

    public override string ToString() => DisplayText;
}
