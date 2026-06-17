using FluentAssertions;
using Platee.Johann.UI.Helpers;

namespace Platee.Johann.Tests.Unit;

public class ReleaseNotesHelperTests
{
    [Fact]
    public void ShouldShow_WhenLastSeenIsNull_ReturnsTrue()
    {
        ReleaseNotesHelper.ShouldShow(null, "1.2.0").Should().BeTrue();
    }

    [Fact]
    public void ShouldShow_WhenLastSeenMatchesCurrent_ReturnsFalse()
    {
        ReleaseNotesHelper.ShouldShow("1.2.0", "1.2.0").Should().BeFalse();
    }

    [Fact]
    public void ShouldShow_WhenLastSeenIsOlder_ReturnsTrue()
    {
        ReleaseNotesHelper.ShouldShow("1.1.0", "1.2.0").Should().BeTrue();
    }

    [Fact]
    public void ShouldShow_WhenLastSeenIsEmpty_ReturnsTrue()
    {
        ReleaseNotesHelper.ShouldShow("", "1.2.0").Should().BeTrue();
    }

    [Fact]
    public void RenderToHtml_WrapsMarkdownInStyledDocument()
    {
        var html = ReleaseNotesHelper.RenderToHtml("# Test\n\nHello");

        html.Should().Contain("<html");
        html.Should().Contain("<h1");
        html.Should().Contain("Hello");
        html.Should().Contain("font-family");
    }
}
