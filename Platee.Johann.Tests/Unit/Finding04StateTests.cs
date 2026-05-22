namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UI;
using Platee.Johann.UI.ViewModels;

public sealed class Finding04StateTests
{
    [Fact]
    public void FindMissingInputPathIssue_WhenInputIssueExists_ReturnsConfiguredInputIssue()
    {
        var issues = new[]
        {
            new StartupPathIssue("Ausgabeverzeichnis", @"C:\bad-output", @"C:\fallback-output", "missing"),
            new StartupPathIssue("Quellverzeichnis", @"C:\bad-input", @"C:\fallback-input", "missing"),
        };

        var result = Finding04State.FindMissingInputPathIssue(issues);

        result.Should().NotBeNull();
        result!.ConfiguredPath.Should().Be(@"C:\bad-input");
        result.FallbackPath.Should().Be(@"C:\fallback-input");
    }

    [Fact]
    public void FindMissingInputPathIssue_WhenInputIssueDoesNotExist_ReturnsNull()
    {
        var issues = new[]
        {
            new StartupPathIssue("Ausgabeverzeichnis", @"C:\bad-output", @"C:\fallback-output", "missing"),
        };

        var result = Finding04State.FindMissingInputPathIssue(issues);

        result.Should().BeNull();
    }

    [Theory]
    [InlineData(false, false, false, Finding04State.NoEntryDisabledReason)]
    [InlineData(false, true, false, Finding04State.NoEntryDisabledReason)]
    [InlineData(true, false, false, Finding04State.NoApiKeyDisabledReason)]
    [InlineData(true, true, true, "")]
    public void DetailActionState_UsesEntryAndApiAvailability(bool hasEntry, bool canProcess, bool expectedCanUseActions, string expectedReason)
    {
        Finding04State.CanUseDetailActions(hasEntry, canProcess).Should().Be(expectedCanUseActions);
        Finding04State.GetDetailActionDisabledReason(hasEntry, canProcess).Should().Be(expectedReason);
    }
}
