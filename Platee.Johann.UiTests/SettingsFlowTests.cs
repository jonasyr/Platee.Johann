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
/// hint under the checkbox asks; and the restart test really restarts Johann (<c>ctx.Restart()</c>) and reads the template back
/// from the settings window instead of only checking the file.
/// </para>
/// </summary>
[Collection("Desktop")]
public sealed class SettingsFlowTests
{
    /// <summary><c>SettingsViewModel.ModelStatusText</c>, <c>ProbeState.NotFound</c> (SettingsViewModel.cs:244).</summary>
    private const string ModelNotFoundText = "nicht verfügbar";

    /// <summary><c>SettingsViewModel.ModelStatusText</c>, <c>ProbeState.NetworkError</c> (SettingsViewModel.cs:245).</summary>
    private const string ModelNetworkErrorText = "konnte gerade nicht geprüft werden";

    /// <summary>Status after a save to the team file (<c>SettingsViewModel.SavePromptsAsync</c>).</summary>
    private const string GlobalSavedText = "✓ Globale Prompts für alle Mitarbeiter gespeichert.";

    /// <summary>Status after a save to the personal file (<c>SettingsViewModel.SavePromptsAsync</c>).</summary>
    private const string PersonalSavedText = "✓ Persönliche Prompts gespeichert.";

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
        WaitForModelStatus(ctx, ModelNotFoundText);
        ctx.App.Find("Settings.Save").IsEnabled.Should().BeFalse("a model the API does not know blocks saving");

        // times: 1 is exact here — the availability probe uses a plain HttpClient, no retries.
        ctx.Stub.FailNext("/v1/models/", 500);
        ctx.SelectModel("gpt-5.6-terra");
        WaitForModelStatus(ctx, ModelNetworkErrorText);
        ctx.App.Find("Settings.Save").IsEnabled.Should().BeTrue("a failed check says nothing about the model and must not block saving");
        ctx.Stub.Requests.Should().Contain(r => r.Path == "/v1/models/gpt-5.6-terra", "the failed check really ran");
    }

    [Fact]
    public void GlobalSaveTarget_WritesOnlyTheSandboxTeamFile()
    {
        using var ctx = UiTestContext.Start(teamFile: true);

        ctx.OpenSettingsSection("kategorien");
        ctx.SelectComboItem(
            "Settings.SaveTarget",
            item => item.Properties.AutomationId.ValueOrDefault == "Settings.SaveTarget.Global",
            "Settings.SaveTarget.Global");
        ctx.App.Click("Settings.AddCategory");
        ctx.App.Type("Settings.CategoryName", "Teamvorlage Test");
        ctx.App.Type("Settings.CategoryPrompt", "Liste alle Termine auf: {transcript}");
        ctx.App.Find("Settings.CategoryIsGlobal").AsCheckBox().IsChecked = true;

        Save(ctx).Should().Be(GlobalSavedText);
        File.ReadAllText(ctx.Sandbox.TeamPrompts).Should().Contain("Teamvorlage Test");
    }

    /// <summary>
    /// A personal template must stay out of the team file when a later save goes to „Global
    /// (Team)“. Until #114 the global save wrote every category, personal ones included (audit
    /// F29, shipped since v1.4.0); it now writes only global categories to the team file and
    /// the personal ones to <c>prompts.personal.json</c> in the same save.
    /// </summary>
    [Fact]
    public void GlobalSave_DoesNotWritePersonalCategoriesToTeamFile()
    {
        using var ctx = UiTestContext.Start(teamFile: true);

        ctx.OpenSettingsSection("kategorien");
        ctx.App.Click("Settings.AddCategory");
        ctx.App.Type("Settings.CategoryName", "Meine Privatvorlage");
        ctx.App.Type("Settings.CategoryPrompt", "Nur für mich: {transcript}");
        Save(ctx).Should().Be(PersonalSavedText);

        ctx.SelectComboItem(
            "Settings.SaveTarget",
            item => item.Properties.AutomationId.ValueOrDefault == "Settings.SaveTarget.Global",
            "Settings.SaveTarget.Global");
        ctx.App.Click("Settings.AddCategory");
        ctx.App.Type("Settings.CategoryName", "Teamvorlage F29");
        ctx.App.Type("Settings.CategoryPrompt", "Für das Team: {transcript}");
        ctx.App.Find("Settings.CategoryIsGlobal").AsCheckBox().IsChecked = true;
        Save(ctx).Should().Be(GlobalSavedText);

        var teamFile = File.ReadAllText(ctx.Sandbox.TeamPrompts);
        teamFile.Should().Contain("Teamvorlage F29", "the global save itself worked");
        teamFile.Should().NotContain("Meine Privatvorlage", "a personal template belongs in prompts.personal.json only");
    }

    /// <summary>
    /// #114, the half of F29 that lost data: a personal template created in the same session
    /// as a single save with target „Global“ never reached <c>prompts.personal.json</c> — it
    /// lived only in the team file (as a global one) or, after a restart, nowhere as personal.
    /// One save, then both files, then a real restart.
    /// </summary>
    [Fact]
    public void GlobalSave_SingleSave_PutsEachTemplateInItsOwnFile_AndBothSurviveRestart()
    {
        using var ctx = UiTestContext.Start(teamFile: true);
        var personalFile = Path.Combine(ctx.Sandbox.Home, "prompts.personal.json");

        ctx.OpenSettingsSection("kategorien");
        ctx.SelectComboItem(
            "Settings.SaveTarget",
            item => item.Properties.AutomationId.ValueOrDefault == "Settings.SaveTarget.Global",
            "Settings.SaveTarget.Global");
        ctx.App.Click("Settings.AddCategory");
        ctx.App.Type("Settings.CategoryName", "Privat F29");
        ctx.App.Type("Settings.CategoryPrompt", "Nur für mich: {transcript}");
        ctx.App.Click("Settings.AddCategory");
        ctx.App.Type("Settings.CategoryName", "Team F29");
        ctx.App.Type("Settings.CategoryPrompt", "Für das Team: {transcript}");
        ctx.App.Find("Settings.CategoryIsGlobal").AsCheckBox().IsChecked = true;
        Save(ctx).Should().Be(GlobalSavedText);

        var team = File.ReadAllText(ctx.Sandbox.TeamPrompts);
        team.Should().Contain("Team F29");
        team.Should().NotContain("Privat F29", "persönliche Vorlagen gehören nie in die Team-Datei");
        ctx.WaitUntil(
            () => File.Exists(personalFile) && File.ReadAllText(personalFile).Contains("Privat F29", StringComparison.Ordinal),
            UiTimeout,
            "prompts.personal.json enthält die persönliche Vorlage aus demselben Speichern");
        File.ReadAllText(personalFile).Should().NotContain("Team F29", "globale Vorlagen stehen nur in der Team-Datei");

        ctx.Restart();
        ctx.OpenSettingsSection("kategorien");
        ctx.WaitUntil(
            () => CategoryNames(ctx).Contains("Privat F29") && CategoryNames(ctx).Contains("Team F29"),
            UiTimeout,
            "beide Vorlagen sind nach dem Neustart da");
        File.ReadAllText(ctx.Sandbox.TeamPrompts).Should().NotContain("Privat F29", "der Neustart schreibt nichts um");
    }

    /// <summary>
    /// Waits until the probe status next to <c>Settings.ModelPicker</c> (the TextBlock bound to
    /// <c>ModelStatusText</c>, a sibling in the same panel) shows <paramref name="text"/>.
    /// </summary>
    private static void WaitForModelStatus(UiTestContext ctx, string text) =>
        ctx.WaitUntil(
            () => ctx.App.Find("Settings.ModelPicker").Parent.FindAllDescendants()
                .Any(e => (e.Properties.Name.ValueOrDefault ?? string.Empty).Contains(text, StringComparison.Ordinal)),
            UiTimeout,
            $"die Modellprüfung meldet „{text}“");

    /// <summary>
    /// Clicks „Speichern“ once the model check has finished (a running check disables it) and
    /// waits for the status line to report a new „✓“ result — a second save in the same window
    /// must not return the first save's still-visible status.
    /// </summary>
    private static string Save(UiTestContext ctx)
    {
        ctx.WaitUntil(() => ctx.App.Find("Settings.Save").IsEnabled, UiTimeout, "Settings.Save ist aktiv");
        var before = StatusText(ctx);
        ctx.App.Click("Settings.Save");
        ctx.WaitUntil(
            () => StatusText(ctx) is var now && now != before && now.StartsWith('✓'),
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
