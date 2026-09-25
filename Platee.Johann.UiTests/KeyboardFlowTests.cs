namespace Platee.Johann.UiTests;

using FluentAssertions;
using Xunit;

/// <summary>
/// Task 13: keyboard reachability against the real Johann exe — with an entry selected, Tab
/// alone reaches every main button. The audit (F22, docs/audit/2026-09-24-v1.5.0.md) found all
/// buttons reachable, though not in layout order; the order is deliberately not asserted here.
/// </summary>
[Collection("Desktop")]
public sealed class KeyboardFlowTests
{
    [Fact]
    public async Task EveryMainButton_IsReachableByTab()
    {
        using var ctx = UiTestContext.Start();
        ctx.SelectEntry(await ctx.DropDictationAsync("D1"));

        var reached = ctx.TabThroughWindow(maxSteps: 120);

        reached.Should().Contain(["Main.Settings", "Main.ReleaseNotes", "Entries.Dictate", "Detail.Copy", "Detail.Pdf", "Detail.ToggleDone"]);
    }
}
