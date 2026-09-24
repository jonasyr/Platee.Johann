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
}
