namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UI.Helpers;

/// <summary>
/// Doppelklick auf die Trennlinie passt die Eintragsliste an ihren breitesten Eintrag an,
/// wie ein Doppelklick auf die Spaltengrenze in Excel (#96).
/// </summary>
public sealed class ColumnAutoFitTests
{
    [Fact]
    public void The_column_takes_the_widest_row_plus_its_chrome()
    {
        ColumnAutoFit.Width(widestContent: 412, chrome: 26, min: 200, max: 900).Should().Be(438);
    }

    [Fact]
    public void A_fractional_width_is_rounded_up_so_nothing_is_trimmed_by_a_pixel()
    {
        ColumnAutoFit.Width(widestContent: 412.2, chrome: 26, min: 200, max: 900).Should().Be(439);
    }

    [Fact]
    public void Short_rows_never_shrink_the_column_below_its_minimum()
    {
        ColumnAutoFit.Width(widestContent: 90, chrome: 26, min: 200, max: 900).Should().Be(200);
    }

    [Fact]
    public void Very_long_rows_stop_where_the_detail_view_keeps_its_minimum()
    {
        ColumnAutoFit.Width(widestContent: 2000, chrome: 26, min: 200, max: 700).Should().Be(700);
    }

    [Fact]
    public void A_window_too_narrow_for_both_minimums_keeps_the_column_minimum()
    {
        ColumnAutoFit.Width(widestContent: 500, chrome: 26, min: 200, max: 120).Should().Be(200);
    }

    [Fact]
    public void An_empty_list_leaves_the_width_alone()
    {
        ColumnAutoFit.Width(widestContent: 0, chrome: 26, min: 200, max: 900).Should().BeNull();
    }
}
