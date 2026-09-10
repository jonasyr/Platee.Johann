using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Platee.Johann.UI.ViewModels;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Regression cover for #63: marking every entry of a day as done made the day — and with
/// it the user's place in the sidebar — disappear from the date list, because the
/// "Nur unerledigte" filter was applied to days as well as to entries.
/// </summary>
public sealed class DateListFilterTests
{
    [Fact]
    public void Without_the_filter_every_day_stays_visible()
    {
        var days = new[]
        {
            MakeDay(2026, 9, 9, total: 2, pending: 2),
            MakeDay(2026, 6, 18, total: 4, pending: 0),
        };

        var result = DateListFilter.SelectVisible(days, showOnlyPending: false, selectedDate: null);

        result.Visible.Should().HaveCount(2);
        result.HiddenCount.Should().Be(0);
    }

    [Fact]
    public void With_the_filter_days_without_pending_entries_are_hidden()
    {
        var days = new[]
        {
            MakeDay(2026, 9, 9, total: 2, pending: 2),
            MakeDay(2026, 6, 18, total: 4, pending: 0),
        };

        var result = DateListFilter.SelectVisible(days, showOnlyPending: true, selectedDate: null);

        result.Visible.Should().ContainSingle()
            .Which.Date.Should().Be(new DateOnly(2026, 9, 9));
        result.HiddenCount.Should().Be(1);
    }

    [Fact]
    public void The_selected_day_stays_visible_even_when_everything_on_it_is_done()
    {
        var selected = new DateOnly(2026, 6, 18);
        var days = new[]
        {
            MakeDay(2026, 9, 9, total: 2, pending: 2),
            MakeDay(2026, 6, 18, total: 4, pending: 0),
        };

        var result = DateListFilter.SelectVisible(days, showOnlyPending: true, selectedDate: selected);

        result.Visible.Select(d => d.Date).Should().Contain(selected,
            "otherwise finishing a day rips the user out of it and the sidebar looks emptied");
        result.Visible.Should().HaveCount(2);
    }

    [Fact]
    public void The_selected_day_is_not_counted_as_hidden()
    {
        var days = new[]
        {
            MakeDay(2026, 9, 9, total: 2, pending: 2),
            MakeDay(2026, 6, 18, total: 4, pending: 0),
            MakeDay(2026, 5, 22, total: 1, pending: 0),
        };

        var result = DateListFilter.SelectVisible(
            days, showOnlyPending: true, selectedDate: new DateOnly(2026, 6, 18));

        result.HiddenCount.Should().Be(1, "only 22.05. is actually hidden");
    }

    [Fact]
    public void Visible_days_are_ordered_newest_first()
    {
        var days = new[]
        {
            MakeDay(2026, 5, 22, total: 1, pending: 1),
            MakeDay(2026, 9, 9, total: 2, pending: 2),
            MakeDay(2026, 6, 18, total: 4, pending: 3),
        };

        var result = DateListFilter.SelectVisible(days, showOnlyPending: false, selectedDate: null);

        result.Visible.Select(d => d.Date).Should().BeInDescendingOrder();
    }

    [Fact]
    public void A_day_whose_entries_are_all_done_is_hidden_even_when_it_has_entries()
    {
        var days = new[] { MakeDay(2026, 6, 18, total: 9, pending: 0) };

        var result = DateListFilter.SelectVisible(days, showOnlyPending: true, selectedDate: null);

        result.Visible.Should().BeEmpty();
        result.HiddenCount.Should().Be(1);
    }

    private static DateItemViewModel MakeDay(int year, int month, int day, int total, int pending)
    {
        var item = new DateItemViewModel(new DateOnly(year, month, day));
        item.UpdateCounts(total, pending);
        return item;
    }
}
