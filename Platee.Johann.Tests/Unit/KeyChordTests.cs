namespace Platee.Johann.Tests.Unit;

using FlaUI.Core.WindowsAPI;
using FluentAssertions;
using Platee.Johann.UiDriver.Automation;

public sealed class KeyChordTests
{
    [Theory]
    [InlineData("Delete", new[] { VirtualKeyShort.DELETE })]
    [InlineData("Ctrl+Plus", new[] { VirtualKeyShort.CONTROL, VirtualKeyShort.ADD })]
    [InlineData("ctrl+0", new[] { VirtualKeyShort.CONTROL, VirtualKeyShort.KEY_0 })]
    [InlineData("Shift+Tab", new[] { VirtualKeyShort.SHIFT, VirtualKeyShort.TAB })]
    [InlineData("PageDown", new[] { VirtualKeyShort.NEXT })]
    [InlineData("PageUp", new[] { VirtualKeyShort.PRIOR })]
    [InlineData("Home", new[] { VirtualKeyShort.HOME })]
    [InlineData("End", new[] { VirtualKeyShort.END })]
    [InlineData("Left", new[] { VirtualKeyShort.LEFT })]
    [InlineData("Right", new[] { VirtualKeyShort.RIGHT })]
    [InlineData("Up", new[] { VirtualKeyShort.UP })]
    [InlineData("Down", new[] { VirtualKeyShort.DOWN })]
    [InlineData("Ctrl+Home", new[] { VirtualKeyShort.CONTROL, VirtualKeyShort.HOME })]
    [InlineData("F1", new[] { VirtualKeyShort.F1 })]
    [InlineData("F12", new[] { VirtualKeyShort.F12 })]
    [InlineData("F24", new[] { VirtualKeyShort.F24 })]
    public void Parse(string chord, VirtualKeyShort[] expected) =>
        KeyChord.Parse(chord).Should().Equal(expected);

    [Fact]
    public void Parse_Unknown_ThrowsNamingKey() =>
        FluentActions.Invoking(() => KeyChord.Parse("Ctrl+Wurst")).Should().Throw<ArgumentException>().WithMessage("*Wurst*");

    [Fact]
    public void ToEvents_CtrlZero_HoldsCtrlAroundTheKey() =>
        KeyChord.ToEvents(KeyChord.Parse("Ctrl+0"), FakeScan).Should().Equal(
            new KeyEvent(VirtualKeyShort.CONTROL, 0x111, Extended: false, KeyUp: false),
            new KeyEvent(VirtualKeyShort.KEY_0, 0x130, Extended: false, KeyUp: false),
            new KeyEvent(VirtualKeyShort.KEY_0, 0x130, Extended: false, KeyUp: true),
            new KeyEvent(VirtualKeyShort.CONTROL, 0x111, Extended: false, KeyUp: true));

    [Fact]
    public void ToEvents_CtrlShiftTab_ReleasesInReverseOrder() =>
        KeyChord.ToEvents(KeyChord.Parse("Ctrl+Shift+Tab"), FakeScan)
            .Select(e => (e.Key, e.KeyUp))
            .Should().Equal(
                (VirtualKeyShort.CONTROL, false),
                (VirtualKeyShort.SHIFT, false),
                (VirtualKeyShort.TAB, false),
                (VirtualKeyShort.TAB, true),
                (VirtualKeyShort.SHIFT, true),
                (VirtualKeyShort.CONTROL, true));

    [Fact]
    public void ToEvents_CtrlHome_MarksOnlyHomeExtended()
    {
        var events = KeyChord.ToEvents(KeyChord.Parse("Ctrl+Home"), FakeScan);

        events.Where(e => e.Key == VirtualKeyShort.HOME).Should().HaveCount(2).And.OnlyContain(e => e.Extended);
        events.Where(e => e.Key == VirtualKeyShort.CONTROL).Should().HaveCount(2).And.OnlyContain(e => !e.Extended);
    }

    [Fact]
    public void ToEvents_SingleKey_IsDownThenUp() =>
        KeyChord.ToEvents(KeyChord.Parse("Delete"), FakeScan).Should().Equal(
            new KeyEvent(VirtualKeyShort.DELETE, FakeScan(VirtualKeyShort.DELETE), Extended: true, KeyUp: false),
            new KeyEvent(VirtualKeyShort.DELETE, FakeScan(VirtualKeyShort.DELETE), Extended: true, KeyUp: true));

    [Theory]
    [InlineData(VirtualKeyShort.LEFT, true)]
    [InlineData(VirtualKeyShort.RIGHT, true)]
    [InlineData(VirtualKeyShort.UP, true)]
    [InlineData(VirtualKeyShort.DOWN, true)]
    [InlineData(VirtualKeyShort.HOME, true)]
    [InlineData(VirtualKeyShort.END, true)]
    [InlineData(VirtualKeyShort.PRIOR, true)]
    [InlineData(VirtualKeyShort.NEXT, true)]
    [InlineData(VirtualKeyShort.INSERT, true)]
    [InlineData(VirtualKeyShort.DELETE, true)]
    [InlineData(VirtualKeyShort.DIVIDE, true)]
    [InlineData(VirtualKeyShort.RCONTROL, true)]
    [InlineData(VirtualKeyShort.RMENU, true)]
    [InlineData(VirtualKeyShort.CONTROL, false)]
    [InlineData(VirtualKeyShort.SHIFT, false)]
    [InlineData(VirtualKeyShort.ADD, false)]
    [InlineData(VirtualKeyShort.SUBTRACT, false)]
    [InlineData(VirtualKeyShort.KEY_0, false)]
    [InlineData(VirtualKeyShort.TAB, false)]
    public void IsExtended(VirtualKeyShort key, bool expected) =>
        KeyChord.IsExtended(key).Should().Be(expected);

    private static ushort FakeScan(VirtualKeyShort key) => (ushort)((ushort)key + 0x100);
}
