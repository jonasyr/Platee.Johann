namespace Platee.Johann.UI.ViewModels;

/// <summary>
/// Decides which days the date sidebar shows.
/// </summary>
public static class DateListFilter
{
    /// <summary>
    /// Applies the "Nur unerledigte" filter to the date list.
    /// <para>
    /// The filter hides days that have nothing left to do, but never the day the user is
    /// currently looking at: finishing the last entry of a day would otherwise pull that day
    /// out from under the selection and make the sidebar look as if data had been lost.
    /// The caller surfaces <see cref="DateListSelection.HiddenCount"/> so the shortened list
    /// is explained rather than merely shorter.
    /// </para>
    /// </summary>
    public static DateListSelection SelectVisible(
        IReadOnlyList<DateItemViewModel> allDates,
        bool showOnlyPending,
        DateOnly? selectedDate)
    {
        var visible = allDates
            .Where(d => !showOnlyPending || d.PendingCount > 0 || d.Date == selectedDate)
            .OrderByDescending(d => d.Date)
            .ToList();

        return new DateListSelection(visible, allDates.Count - visible.Count);
    }
}

/// <summary>
/// Result of applying the "Nur unerledigte" filter to the date sidebar.
/// </summary>
public sealed record DateListSelection(
    IReadOnlyList<DateItemViewModel> Visible,
    int HiddenCount);
