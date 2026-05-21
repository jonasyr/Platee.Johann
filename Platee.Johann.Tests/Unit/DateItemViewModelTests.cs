namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UI.ViewModels;

public sealed class DateItemViewModelTests
{
    [Fact]
    public void DisplayText_formats_date_without_year()
    {
        var sut = new DateItemViewModel(new DateOnly(2026, 3, 31));

        sut.DisplayText.Should().Be("31.03.");
    }

    [Fact]
    public void PendingCount_change_raises_property_changed_for_AllDone()
    {
        var sut = new DateItemViewModel(new DateOnly(2026, 3, 31));
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.PendingCount = 1;

        raised.Should().Contain(nameof(DateItemViewModel.PendingCount));
        raised.Should().Contain(nameof(DateItemViewModel.AllDone));
        raised.Should().NotContain(nameof(DateItemViewModel.DisplayText));
    }
}
