namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UiDriver.Automation;

public sealed class JohannWindowsKeyTargetTests
{
    [Fact]
    public void SelectKeyTargetIndex_MainAndUntitledPopup_PicksMain()
    {
        var windows = new (string Title, bool IsModal, bool IsMain)[]
        {
            ("Platé.Johann 1.5.0", IsModal: false, IsMain: true),
            (string.Empty, IsModal: false, IsMain: false),
        };

        JohannWindows.SelectKeyTargetIndex(windows).Should().Be(0);
    }

    [Fact]
    public void SelectKeyTargetIndex_MainAndNonModalSettings_PicksMain_NotSettings()
    {
        var windows = new (string Title, bool IsModal, bool IsMain)[]
        {
            ("Platé.Johann 1.5.0", IsModal: false, IsMain: true),
            ("Einstellungen – Platé.Johann", IsModal: false, IsMain: false),
        };

        JohannWindows.SelectKeyTargetIndex(windows).Should().Be(0);
    }

    [Fact]
    public void SelectKeyTargetIndex_MainAndModalMessageBox_PicksMessageBox()
    {
        var windows = new (string Title, bool IsModal, bool IsMain)[]
        {
            ("Platé.Johann 1.5.0", IsModal: false, IsMain: true),
            ("Platé.Johann – Eintrag löschen?", IsModal: true, IsMain: false),
        };

        JohannWindows.SelectKeyTargetIndex(windows).Should().Be(1);
    }

    [Fact]
    public void SelectKeyTargetIndex_MainAndUntitledModal_NeverPicksTheUntitledOne()
    {
        // A WPF popup/tooltip can report IsModal too (or the property can be unreliable for
        // non-Window chrome) — an empty title must disqualify it regardless of IsModal.
        var windows = new (string Title, bool IsModal, bool IsMain)[]
        {
            ("Platé.Johann 1.5.0", IsModal: false, IsMain: true),
            (string.Empty, IsModal: true, IsMain: false),
        };

        JohannWindows.SelectKeyTargetIndex(windows).Should().Be(0);
    }

    [Fact]
    public void SelectKeyTargetIndex_NoMainAndNoModal_ReturnsMinusOne_MeaningFallBackToMainWindow()
    {
        var windows = new (string Title, bool IsModal, bool IsMain)[]
        {
            ("Einstellungen – Platé.Johann", IsModal: false, IsMain: false),
            (string.Empty, IsModal: false, IsMain: false),
        };

        JohannWindows.SelectKeyTargetIndex(windows).Should().Be(-1);
    }

    [Fact]
    public void SelectKeyTargetIndex_EmptyList_ReturnsMinusOne() =>
        JohannWindows.SelectKeyTargetIndex(Array.Empty<(string Title, bool IsModal, bool IsMain)>())
            .Should().Be(-1);
}
