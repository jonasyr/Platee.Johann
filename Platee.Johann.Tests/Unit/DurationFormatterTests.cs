namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UI.Helpers;

public sealed class DurationFormatterTests
{
    [Theory]
    [InlineData(0, "0:00")]
    [InlineData(59, "0:59")]
    [InlineData(60, "1:00")]
    [InlineData(90, "1:30")]
    [InlineData(3599, "59:59")]
    [InlineData(3600, "1:00:00")]
    [InlineData(3661, "1:01:01")]
    public void Format_ReturnsExpectedString(double seconds, string expected)
    {
        DurationFormatter.Format(seconds).Should().Be(expected);
    }
}
