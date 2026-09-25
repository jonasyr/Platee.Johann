namespace Platee.Johann.UiTests;

using System.IO;
using FlaUI.Core.AutomationElements;
using FluentAssertions;
using Xunit;

/// <summary>
/// Task 13: settings flows against the real Johann exe — a personal template survives a
/// restart, a missing model blocks saving while a failed check does not, and saving to
/// „Global (Team)“ writes only the sandbox's own team file.
/// <para>
/// Deviations from the brief, each checked against the app (details in the Task 13 report):
/// section ids use the lower-case keys of <c>SettingsViewModel</c> (<c>kategorien</c> for
/// „Vorlagen“, <c>ki-modell</c>); combo boxes are driven through the SelectionItem pattern
/// instead of mouse clicks into their popup; every assertion after an asynchronous step (model
/// check, save) polls; the new template of the team test is also marked „Global (Team)“, as the
/// hint under the checkbox asks; the personal file may not exist at all after a team save; and
/// the restart test really restarts Johann (<c>ctx.Restart()</c>) and reads the template back
/// from the settings window instead of only checking the file.
/// </para>
/// </summary>
[Collection("Desktop")]
public sealed class SettingsFlowTests
{
    private static readonly TimeSpan UiTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public void PersonalCategory_SurvivesRestart()
    {
        using var ctx = UiTestContext.Start();
        var personalFile = Path.Combine(ctx.Sandbox.Home, "prompts.personal.json");

        ctx.OpenSettingsSection("kategorien");
        ctx.App.Click("Settings.AddCategory");
        ctx.App.Type("Settings.CategoryName", "Baustellenbericht");
        ctx.App.Type("Settings.CategoryPrompt", "Fasse den Baustellenstand zusammen: {transcript}");
        var status = Save(ctx);

        ctx.WaitUntil(
            () => File.Exists(personalFile) && File.ReadAllText(personalFile).Contains("Baustellenbericht", StringComparison.Ordinal),
            UiTimeout,
            $"prompts.personal.json enthält die neue Vorlage (Status nach dem Speichern: „{status}“)");

        ctx.Restart();
        ctx.OpenSettingsSection("kategorien");
        ctx.WaitUntil(
            () => CategoryNames(ctx).Contains("Baustellenbericht"),
            UiTimeout,
            "Settings.CategoryList zeigt nach dem Neustart „Baustellenbericht“");
    }

    [Fact]
    public void MissingModel_BlocksSave_NetworkErrorDoesNot()
    {
        using var ctx = UiTestContext.Start(stub: s => s.MissingModel("gpt-5.6-sol"));
        ctx.OpenSettingsSection("ki-modell");

        ctx.SelectModel("gpt-5.6-sol");
        ctx.WaitUntil(
            () => ctx.App.Find("Settings.ModelPicker").Parent.FindAllDescendants()
                .Any(e => (e.Properties.Name.ValueOrDefault ?? string.Empty).Contains("nicht verfügbar", StringComparison.Ordinal)),
            UiTimeout,
            "die Prüfung meldet „✗ nicht verfügbar“");
        ctx.App.Find("Settings.Save").IsEnabled.Should().BeFalse("a model the API does not know blocks saving");

        ctx.Stub.FailNext("/v1/models/", 500);
        ctx.SelectModel("gpt-5.6-terra");
        ctx.WaitUntil(
            () => ctx.App.Find("Settings.Save").IsEnabled,
            UiTimeout,
            "Settings.Save wird nach einem Netzwerkfehler der Prüfung wieder aktiv");
        ctx.Stub.Requests.Should().Contain(r => r.Path == "/v1/models/gpt-5.6-terra", "the failed check really ran");
    }

    [Fact]
    public void GlobalSaveTarget_WritesOnlyTheSandboxTeamFile()
    {
        using var ctx = UiTestContext.Start(teamFile: true);
        var personalFile = Path.Combine(ctx.Sandbox.Home, "prompts.personal.json");

        ctx.OpenSettingsSection("kategorien");
        ctx.SelectComboItem(
            "Settings.SaveTarget",
            item => item.Properties.AutomationId.ValueOrDefault == "Settings.SaveTarget.Global",
            "Settings.SaveTarget.Global");
        ctx.App.Click("Settings.AddCategory");
        ctx.App.Type("Settings.CategoryName", "Teamvorlage Test");
        ctx.App.Type("Settings.CategoryPrompt", "Liste alle Termine auf: {transcript}");
        ctx.App.Find("Settings.CategoryIsGlobal").AsCheckBox().IsChecked = true;
        Save(ctx);

        ctx.WaitUntil(
            () => File.ReadAllText(ctx.Sandbox.TeamPrompts).Contains("Teamvorlage Test", StringComparison.Ordinal),
            UiTimeout,
            "die Team-Datei der Sandbox enthält die neue Vorlage");
        ctx.Sandbox.TeamPrompts.Should().StartWith(ctx.Sandbox.Root, "the team file lives inside the sandbox, never on Z:");
        (File.Exists(personalFile) ? File.ReadAllText(personalFile) : string.Empty)
            .Should().NotContain("Teamvorlage Test");
    }

    /// <summary>
    /// Clicks „Speichern“ once the model check has finished (a running check disables it) and
    /// waits for the status line to report the result.
    /// </summary>
    private static string Save(UiTestContext ctx)
    {
        ctx.WaitUntil(() => ctx.App.Find("Settings.Save").IsEnabled, UiTimeout, "Settings.Save ist aktiv");
        ctx.App.Click("Settings.Save");
        ctx.WaitUntil(
            () => StatusText(ctx).StartsWith('✓'),
            UiTimeout,
            "Settings.Status meldet „✓ … gespeichert“");
        return StatusText(ctx);
    }

    private static string StatusText(UiTestContext ctx) =>
        ctx.App.Find("Settings.Status").Properties.Name.ValueOrDefault ?? string.Empty;

    private static IReadOnlyList<string> CategoryNames(UiTestContext ctx) =>
        ctx.App.Find("Settings.CategoryList")
            .FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text))
            .Select(t => t.Properties.Name.ValueOrDefault ?? string.Empty)
            .ToList();
}
