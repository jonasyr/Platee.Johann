namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UiDriver.Automation;

public sealed class JohannWindowsDeduplicateTests
{
    private static readonly IntPtr MainHandle = new(1);
    private static readonly IntPtr DialogHandle = new(2);

    [Fact]
    public void DeduplicateByHandle_KeepsFirstOccurrence_WhenSameHandleAppearsTwice()
    {
        var items = new[]
        {
            (Handle: MainHandle, Title: "Platé.Johann 1.5.0"),
            (Handle: DialogHandle, Title: "Platé.Johann – Neuigkeiten"),
            (Handle: MainHandle, Title: "Platé.Johann 1.5.0 (found again as a descendant)"),
        };

        var result = JohannWindows.DeduplicateByHandle(items, i => i.Handle);

        result.Should().HaveCount(2);
        result[0].Title.Should().Be("Platé.Johann 1.5.0");
        result[1].Title.Should().Be("Platé.Johann – Neuigkeiten");
    }

    [Fact]
    public void DeduplicateByHandle_PutsMainWindowFirstThenDialogs_WhenCallerOrdersThatWay()
    {
        // Mirrors JohannSession.Windows(): top-level windows first, then each Window-typed
        // descendant found while walking the tree.
        var items = new[]
        {
            (Handle: MainHandle, Title: "Platé.Johann 1.5.0"),
            (Handle: DialogHandle, Title: "Platé.Johann – Neuigkeiten"),
        };

        var result = JohannWindows.DeduplicateByHandle(items, i => i.Handle);

        result.Select(i => i.Title).Should().Equal("Platé.Johann 1.5.0", "Platé.Johann – Neuigkeiten");
    }

    [Fact]
    public void DeduplicateByHandle_NeverTreatsZeroHandlesAsDuplicatesOfEachOther()
    {
        var items = new[]
        {
            (Handle: IntPtr.Zero, Title: "First without a native handle"),
            (Handle: IntPtr.Zero, Title: "Second without a native handle"),
        };

        var result = JohannWindows.DeduplicateByHandle(items, i => i.Handle);

        result.Should().HaveCount(2);
    }

    [Fact]
    public void DeduplicateByHandle_EmptyInput_ReturnsEmpty() =>
        JohannWindows.DeduplicateByHandle(Array.Empty<(IntPtr Handle, string Title)>(), i => i.Handle)
            .Should().BeEmpty();
}
