namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UiDriver.Tool;

public sealed class CommandArgsTests
{
    [Fact]
    public void PositionalsExcluding_SkipsValueOptionBeforePositional_LikeScreenshotWithOf() =>
        CommandArgs.PositionalsExcluding(["--of", "btnOk", "out.png"], "--of")
            .Should().Equal("out.png");

    [Fact]
    public void PositionalsExcluding_SkipsValueOptionAfterPositional_LikeWaitForWithTimeout() =>
        CommandArgs.PositionalsExcluding(["myId", "--timeout", "5"], "--timeout")
            .Should().Equal("myId");

    [Fact]
    public void PositionalsExcluding_TreatsUnlistedDoubleDashTokenAsBareFlag_LikeClickWithMouse() =>
        CommandArgs.PositionalsExcluding(["--mouse", "myId"])
            .Should().Equal("myId");

    [Fact]
    public void PositionalsExcluding_WithoutValueOptionName_TreatsItsValueAsPositional_RegressionForI3()
    {
        // Reproduces the I3 defect directly: --of is not declared as a value-option, so its value
        // "btnOk" is wrongly treated as a positional instead of being skipped.
        var result = CommandArgs.PositionalsExcluding(["--of", "btnOk", "out.png"]);

        result.Should().Equal("btnOk", "out.png");
    }

    [Fact]
    public void RequireAt_MissingIndex_ThrowsWithDescription() =>
        FluentActions.Invoking(() => CommandArgs.RequireAt([], 0, "id|name"))
            .Should().Throw<ArgumentException>().WithMessage("*id|name*");

    [Fact]
    public void Option_ReturnsValueFollowingName() =>
        CommandArgs.Option(["--timeout", "5"], "--timeout").Should().Be("5");

    [Fact]
    public void Flag_DetectsBareFlagAnywhere() =>
        CommandArgs.Flag(["myId", "--mouse"], "--mouse").Should().BeTrue();
}
