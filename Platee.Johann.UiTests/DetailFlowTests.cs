namespace Platee.Johann.UiTests;

using System.IO;
using System.Text.Json.Nodes;
using FlaUI.Core.AutomationElements;
using FluentAssertions;
using Platee.Johann.Application.Settings;
using Platee.Johann.UiDriver.Automation;
using Xunit;

/// <summary>
/// Task 12: detail-view flows against the real Johann exe — copy all / copy one section,
/// section ticks, transcript edit + regeneration, an on-demand section, the PDF export and zoom.
/// <para>
/// Deviations from the brief, each checked against the app (details in the Task 12 report):
/// the copy headings are upper case (<c>EntryDetailViewModel.CopyParts</c>:
/// <c>DisplayNameOf(id).ToUpperInvariant()</c>, transcript "ORIGINALTRANSKRIPT"); selecting an
/// entry sets the section ticks to its type (<c>MainViewModel.OnSelectedEntryChanged</c>), so for
/// the "Projekt" fixtures D1/D3 "Aufgaben" and "Stundenzettel" start unticked; the sandbox has no
/// <c>sectionModes</c>, which makes every built-in Auto (<c>SectionCatalog.Build</c>), so the
/// on-demand test configures the Recommended preset; built-ins are generated on demand only from
/// the "Neu generieren" context menu; the PDF is rendered via "PDF in Zwischenablage kopieren",
/// because "PDF" opens the file in the default viewer.
/// </para>
/// <para>
/// A section's presence is probed via its copy icon <c>Copy.&lt;id&gt;</c>, not
/// <c>Section.&lt;id&gt;</c>: that id sits on a <c>ContentControl</c>, which has no automation
/// peer, so UIA never exposes it (found live). The icon exists exactly when the section is shown
/// with text — it is collapsed while <c>CanCopySection</c> is false.
/// </para>
/// </summary>
[Collection("Desktop")]
public sealed class DetailFlowTests
{
    private const string ChatPath = "/v1/chat/completions";
    private const string EditedTranscript = "Korrigierter Text über Peano.";
    private const string EditedAnswer = "Korrigierte Fassung.";

    /// <summary>
    /// Phrase that occurs only in D1's task list (Fixtures/D1_status.json) — the task lines
    /// themselves also appear in the transcript and the prose summary.
    /// </summary>
    private const string D1TaskPhrase = "Die Beteiligten sind die Bauherrin";

    /// <summary>Window title of <c>EmptySectionHintDialog</c>.</summary>
    private const string EmptySectionHintTitle = "Platé.Johann – Hinweis";

    private static readonly TimeSpan UiTimeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task HidingSection_RemovesItFromCopy()
    {
        using var ctx = UiTestContext.Start();
        await SelectFixtureAsync(ctx, "D1");

        SetSectionShown(ctx, "Sections.ShowTaskList", "Copy.builtin.taskList", shown: true);
        CopyVia(ctx, "Detail.Copy").Should().Contain("AUFGABEN").And.Contain(D1TaskPhrase);

        SetSectionShown(ctx, "Sections.ShowTaskList", "Copy.builtin.taskList", shown: false);
        var text = CopyVia(ctx, "Detail.Copy");
        text.Should().NotContain("AUFGABEN").And.NotContain(D1TaskPhrase).And.NotContain("**");
        text.Should().Contain("ZUSAMMENFASSUNG", "the rest of the entry is still copied");
    }

    [Fact]
    public async Task SectionCopySymbol_CopiesOnlyThatSection()
    {
        using var ctx = UiTestContext.Start();
        await SelectFixtureAsync(ctx, "D1");
        SetSectionShown(ctx, "Sections.ShowTaskList", "Copy.builtin.taskList", shown: true);

        var text = CopyVia(ctx, "Copy.builtin.taskList");

        text.Should().StartWith("AUFGABEN").And.Contain(D1TaskPhrase);
        text.Should().NotContain("ORIGINALTRANSKRIPT").And.NotContain("ZUSAMMENFASSUNG");
    }

    [Fact]
    public async Task EditTranscript_Regenerate_SendsEditedText()
    {
        using var ctx = UiTestContext.Start(
            chatOverride: content => content.Contains("Korrigierte", StringComparison.Ordinal) ? EditedAnswer : null);
        await SelectFixtureAsync(ctx, "D6");

        ctx.App.Click("Detail.EditTranscript");
        ctx.App.Type("Detail.TranscriptEditor", EditedTranscript);
        ctx.WaitUntil(
            () => ctx.App.Find("Detail.TranscriptEditor").AsTextBox().Text == EditedTranscript,
            UiTimeout,
            "der Editor enthält den korrigierten Text");
        ctx.App.Click("Detail.RegenerateFromTranscript");

        // End state: the regenerated entry is on disk — the edited transcript and the stub's
        // answer to it — so every request of this run has been sent.
        ctx.WaitUntil(
            () => StatusFiles(ctx).Any(s =>
                s["editedTranscript"]?.GetValue<string>() == EditedTranscript
                && s["longSummary"]?.GetValue<string>() == EditedAnswer),
            TimeSpan.FromSeconds(60),
            "der Eintrag ist aus dem bearbeiteten Transkript neu generiert und gespeichert");
        await ctx.WaitUntilIdleAsync();

        // The raw body is JSON with non-ASCII escaped ("ü" as \u00FC), so the text is compared
        // after decoding the messages, not in the raw body.
        ChatContents(ctx).Should().Contain(c => c.Contains(EditedTranscript, StringComparison.Ordinal));
    }

    [Fact]
    public async Task OnDemandSection_IsGeneratedOnClick()
    {
        using var ctx = UiTestContext.Start(adjustSettings: WithRecommendedSectionModes);
        await SelectFixtureAsync(ctx, "D3");

        // Ticking a section the entry has no text for opens a one-off modal hint
        // ("Vorlage wurde für diesen Eintrag nicht erzeugt", EmptySectionHint) that names this
        // very workaround — the context menu of "Neu generieren". It is closed with "OK".
        // Ticked, but without text the section is still not shown (ShowStundenzettelSection).
        ClickCheckBox(ctx, "Sections.ShowStundenzettelText", isChecked: true, expectEmptySectionHint: true);
        ctx.App.TryFind("Copy.builtin.stundenzettel", TimeSpan.FromSeconds(1))
            .Should().BeNull("Stundenzettel is OnDemand under the Recommended preset and not generated yet");

        var before = ctx.Stub.Requests.Count;
        OpenContextMenu(ctx, "Detail.Reprocess");
        InvokeMenuItem(ctx, "Generate.builtin.stundenzettel");

        ctx.WaitUntil(
            () => ctx.App.TryFind("Copy.builtin.stundenzettel", TimeSpan.Zero) is not null,
            TimeSpan.FromSeconds(30),
            "der Abschnitt Stundenzettel erscheint nach dem Generieren");
        await ctx.WaitUntilIdleAsync();

        ctx.Stub.Requests.Count.Should().Be(before + 1);
        ctx.Stub.Requests[^1].Path.Should().Be(ChatPath);
    }

    [Fact]
    public async Task Pdf_ContainsSections_WithoutMarkdownStars()
    {
        using var ctx = UiTestContext.Start();
        await SelectFixtureAsync(ctx, "D1");
        SetSectionShown(ctx, "Sections.ShowTaskList", "Copy.builtin.taskList", shown: true);

        var clickedAt = DateTime.UtcNow;
        OpenContextMenu(ctx, "Detail.Pdf");
        InvokeMenuItem(ctx, "Detail.CopyPdf");

        var pdf = await ctx.WaitForFileAsync(ctx.Sandbox.Output, "*.pdf", changedAfterUtc: clickedAt);
        var text = string.Empty;
        ctx.WaitUntil(
            () => (text = PdfText.Extract(pdf)).Length > 0,
            UiTimeout,
            "die exportierte PDF ist lesbar");

        text.Should().Contain("Aufgaben").And.Contain(D1TaskPhrase).And.NotContain("**");
    }

    [Fact]
    public async Task Zoom_KeysChangeZoomText()
    {
        using var ctx = UiTestContext.Start();
        await SelectFixtureAsync(ctx, "D1");

        ctx.App.Key("Ctrl+Plus");
        ctx.WaitUntil(() => ZoomText(ctx) == "110 %", UiTimeout, "Detail.ZoomText zeigt 110 %");

        ctx.App.Key("Ctrl+0");
        ctx.WaitUntil(() => ZoomText(ctx) == "100 %", UiTimeout, "Detail.ZoomText zeigt 100 %");
        ZoomText(ctx).Should().Be("100 %");
    }

    /// <summary>Drops the fixture, selects its entry and waits until the detail view shows it.</summary>
    private static async Task SelectFixtureAsync(UiTestContext ctx, string fixture)
    {
        ctx.SelectEntry(await ctx.DropDictationAsync(fixture));
        ctx.WaitUntil(
            () => ctx.App.TryFind("Copy.builtin.longSummary", TimeSpan.Zero) is not null,
            UiTimeout,
            $"die Detailansicht zeigt den Eintrag {fixture}");
    }

    private static void SetSectionShown(UiTestContext ctx, string checkBoxId, string sectionId, bool shown)
    {
        ClickCheckBox(ctx, checkBoxId, shown);
        ctx.WaitUntil(
            () => (ctx.App.TryFind(sectionId, TimeSpan.Zero) is not null) == shown,
            UiTimeout,
            $"{sectionId} ist {(shown ? "sichtbar" : "ausgeblendet")}");
    }

    /// <summary>
    /// Clicks the checkbox once and waits for the expected state — it must start in the other
    /// state, which makes the type-based defaults this relies on explicit.
    /// </summary>
    private static void ClickCheckBox(UiTestContext ctx, string checkBoxId, bool isChecked, bool expectEmptySectionHint = false)
    {
        ctx.App.Find(checkBoxId).AsCheckBox().IsChecked.Should().Be(!isChecked, $"{checkBoxId} starts in the other state");
        ctx.App.Click(checkBoxId);
        if (expectEmptySectionHint)
        {
            ctx.App.Find(EmptySectionHintTitle, UiTimeout).Should().NotBeNull("the hint explains the empty section");
            ctx.App.Click("OK", mouse: true);
            ctx.WaitUntil(
                () => ctx.App.TryFind(EmptySectionHintTitle, TimeSpan.Zero) is null,
                UiTimeout,
                "der Hinweis ist geschlossen");
        }

        ctx.WaitUntil(
            () => ctx.App.Find(checkBoxId).AsCheckBox().IsChecked == isChecked,
            UiTimeout,
            $"{checkBoxId} ist {(isChecked ? "angehakt" : "nicht angehakt")}");
    }

    /// <summary>
    /// Clicks <paramref name="buttonId"/> and returns the clipboard once it has changed. A UIA
    /// Invoke on a WPF button runs its command asynchronously, so the clipboard is set to a
    /// sentinel first and polled until the app has replaced it.
    /// </summary>
    private static string CopyVia(UiTestContext ctx, string buttonId)
    {
        var sentinel = $"ui-test-sentinel-{Guid.NewGuid():N}";
        JohannSession.WriteClipboard(sentinel);
        ctx.App.Click(buttonId);

        var text = sentinel;
        ctx.WaitUntil(
            () => (text = JohannSession.ReadClipboard()) != sentinel && text.Length > 0,
            UiTimeout,
            $"{buttonId} legt Text in die Zwischenablage");
        return text;
    }

    /// <summary>
    /// Opens the context menu of <paramref name="elementId"/> from the keyboard (focus, then
    /// Shift+F10), the way WPF offers it to keyboard users. Not with a right-click: found live
    /// (Task 12), a <c>RightClick</c> on <c>Detail.Pdf</c> right after a mouse click on a sidebar
    /// checkbox left the cursor at the right height but some 640 px too far left, on this
    /// multi-monitor desktop with negative coordinates — the menu never opened.
    /// </summary>
    private static void OpenContextMenu(UiTestContext ctx, string elementId)
    {
        ctx.App.Find(elementId).Focus();
        ctx.App.Key("Shift+F10");
    }

    /// <summary>
    /// Invokes a context-menu item. A WPF context menu is its own popup window, which is not a
    /// UIA <c>Window</c>, so the item is searched in every top-level element of Johann's process.
    /// </summary>
    private static void InvokeMenuItem(UiTestContext ctx, string itemId)
    {
        AutomationElement? item = null;
        ctx.WaitUntil(
            () =>
            {
                var desktop = ctx.App.MainWindow.Automation.GetDesktop();
                item = desktop.FindAllChildren(cf => cf.ByProcessId(ctx.App.ProcessId))
                    .Select(root => root.FindFirstDescendant(cf => cf.ByAutomationId(itemId)))
                    .FirstOrDefault(found => found is not null);
                return item is not null;
            },
            UiTimeout,
            $"Kontextmenüeintrag {itemId} erscheint");
        item!.Patterns.Invoke.Pattern.Invoke();
    }

    private static string? ZoomText(UiTestContext ctx) => ctx.App.Find("Detail.ZoomText").Properties.Name.ValueOrDefault;

    /// <summary>All message contents of every chat request, decoded from the JSON body.</summary>
    private static IEnumerable<string> ChatContents(UiTestContext ctx) =>
        ctx.Stub.Requests
            .Where(r => r.Path == ChatPath)
            .SelectMany(r => JsonNode.Parse(r.Body)?["messages"]?.AsArray() ?? [])
            .Select(message => message?["content"]?.GetValue<string>() ?? string.Empty);

    private static IEnumerable<JsonNode> StatusFiles(UiTestContext ctx) =>
        Directory.EnumerateFiles(ctx.Sandbox.Output, "*_status.json", SearchOption.AllDirectories)
            .Where(path => !path.Contains("_Papierkorb", StringComparison.Ordinal))
            .Select(path => JsonNode.Parse(File.ReadAllText(path)))
            .OfType<JsonNode>();

    private static JsonObject WithRecommendedSectionModes(JsonObject settings)
    {
        var modes = new JsonObject();
        foreach (var (id, mode) in SectionModeDefaults.Recommended)
        {
            modes[id] = mode.ToString();
        }

        settings["sectionModes"] = modes;
        return settings;
    }
}
