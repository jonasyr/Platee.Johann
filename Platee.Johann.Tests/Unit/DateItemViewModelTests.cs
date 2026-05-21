namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UI.ViewModels;

public sealed class DateItemViewModelTests
{
    [Fact]
    public void DisplayText_formats_date_and_hides_zero_pending_count()
    {
        var sut = new DateItemViewModel(new DateOnly(2026, 3, 31));

        sut.DisplayText.Should().Be("31.03.26");

        sut.PendingCount = 3;
        sut.DisplayText.Should().Be("31.03.26 (3)");
    }

    [Fact]
    public void PendingCount_change_raises_property_changed_for_DisplayText()
    {
        var sut = new DateItemViewModel(new DateOnly(2026, 3, 31));
        var raised = new List<string?>();
        sut.PropertyChanged += (_, e) => raised.Add(e.PropertyName);

        sut.PendingCount = 1;

        raised.Should().Contain(nameof(DateItemViewModel.PendingCount));
        raised.Should().Contain(nameof(DateItemViewModel.DisplayText));
    }
}
