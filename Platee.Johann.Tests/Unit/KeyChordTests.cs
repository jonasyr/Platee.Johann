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
    public void Parse(string chord, VirtualKeyShort[] expected) =>
        KeyChord.Parse(chord).Should().Equal(expected);

    [Fact]
    public void Parse_Unknown_ThrowsNamingKey() =>
        FluentActions.Invoking(() => KeyChord.Parse("Ctrl+Wurst")).Should().Throw<ArgumentException>().WithMessage("*Wurst*");
}
