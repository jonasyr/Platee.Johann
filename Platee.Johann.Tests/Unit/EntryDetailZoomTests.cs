using FluentAssertions;
using Platee.Johann.UI.ViewModels;

namespace Platee.Johann.Tests.Unit;

public sealed class EntryDetailZoomTests
{
    private static EntryDetailViewModel CreateVm()
    {
        return new EntryDetailViewModel([], string.Empty);
    }

    [Fact]
    public void ZoomReset_SetsZoomToOneHundredPercent()
    {
        var vm = CreateVm();
        vm.ZoomInCommand.Execute(null); // 1.1
        vm.ZoomInCommand.Execute(null); // 1.2

        vm.ZoomResetCommand.Execute(null);

        vm.DetailZoom.Should().Be(1.0);
        vm.ZoomText.Should().Be("100 %");
    }

    [Fact]
    public void ZoomIn_ClampsAtMaximum()
    {
        var vm = CreateVm();
        for (var i = 0; i < 15; i++) vm.ZoomInCommand.Execute(null);

        vm.DetailZoom.Should().Be(2.0);
    }

    [Fact]
    public void ZoomOut_ClampsAtMinimum()
    {
        var vm = CreateVm();
        for (var i = 0; i < 15; i++) vm.ZoomOutCommand.Execute(null);

        vm.DetailZoom.Should().Be(0.5);
    }

    [Fact]
    public void ZoomReset_AfterZoomOut_RestoresToDefault()
    {
        var vm = CreateVm();
        vm.ZoomOutCommand.Execute(null); // 0.9
        vm.ZoomOutCommand.Execute(null); // 0.8

        vm.ZoomResetCommand.Execute(null);

        vm.DetailZoom.Should().Be(1.0);
    }
}
