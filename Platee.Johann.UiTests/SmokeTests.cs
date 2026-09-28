namespace Platee.Johann.UiTests;

using FluentAssertions;
using Platee.Johann.UiDriver.Automation;
using Xunit;

/// <summary>
/// Phase-B smoke tests (#111): start the real Johann exe against a stubbed OpenAI endpoint and an
/// isolated sandbox, and check the very first things a user would see. Deliberately left unrun by
/// this task — see the hard constraint in the task-10 brief — this is TDD "RED" in the sense that
/// it compiles but has never executed; `scripts/run-ui-tests.ps1` runs it later, on a machine
/// where nobody is using the desktop.
/// </summary>
[Collection("Desktop")]
public sealed class SmokeTests
{
    [Fact]
    public void Starts_WithoutDialogs_AndShowsEmptyLists()
    {
        using var ctx = UiTestContext.Start();
        ctx.App.Find("Entries.Dictate").IsEnabled.Should().BeTrue();
        ctx.Stub.Requests.Should().OnlyContain(r => r.Path.StartsWith("/v1/models/"));
    }

    [Fact]
    public void FirstRun_ShowsReleaseNotes()
    {
        using var ctx = UiTestContext.Start(firstRun: true);
        ctx.App.Windows().Should().Contain(w => w.Title.Contains("Neuigkeiten") || w.Title.Contains("Was ist neu"));
    }

    [Fact]
    public void RefusesToStart_WhenJohannAlreadyRuns()
    {
        using var first = UiTestContext.Start();
        var act = () => UiTestContext.Start();
        act.Should().Throw<InvalidOperationException>().WithMessage("*läuft bereits*");
    }

    [Fact]
    public void UnexpectedDialog_IsReported_NotWaitedOut()
    {
        // Ausgabeordner auf ein nicht anlegbares Ziel → Pfadwarnung beim Start
        var act = () => UiTestContext.Start(adjustSettings: json =>
        {
            json["ausgabeverzeichnis"] = @"Q:\gibt-es-nicht\output";
            return json;
        });

        act.Should().Throw<UnexpectedWindowException>().Which.Title.Should().Contain("Verzeichnisse");
    }
}
