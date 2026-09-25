namespace Platee.Johann.UiTests;

using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using FlaUI.Core.AutomationElements;
using Platee.Johann.Application.Processing;
using Platee.Johann.Domain.Parsing;
using Platee.Johann.UiDriver.Automation;
using Platee.Johann.UiDriver.Sandbox;
using Platee.Johann.UiDriver.Stub;

/// <summary>
/// Everything one UI smoke/scenario test needs: an isolated sandbox, a stubbed OpenAI endpoint
/// with sensible defaults, and a launched Johann session — created and torn down as a single
/// unit so every test starts from a clean desktop.
/// <para>
/// <b>Default stub behaviour</b> (used unless a test's own <c>stub</c> callback overrides it):
/// transcription answers with the matching fixture's <c>D&lt;n&gt;_status.json</c> transcript
/// when that file exists (added by the audit, Task 8+), otherwise with the plain text of
/// <c>D&lt;n&gt;.txt</c>. Chat answers are matched by request prefix via
/// <see cref="SectionPromptMatcher"/>: a title request gets the first five words of the
/// dictation's transcript; a section request gets the fixture's own section text when
/// <c>D&lt;n&gt;_status.json</c> has one, otherwise a short deterministic German markdown
/// placeholder (<c>"**Test** &lt;section&gt;\n- Punkt 1\n  - Unterpunkt"</c>) — good enough to
/// exercise rendering (markdown, nesting) without depending on fixtures that do not exist yet.
/// </para>
/// </summary>
public sealed class UiTestContext : IDisposable
{
    /// <summary>
    /// A chat request only counts as matching one of a fixture's own already-generated section
    /// outputs (see <see cref="ApplyDefaultStubRules"/>'s third pass) once that output is at least
    /// this long — a short section value could otherwise appear as a coincidental substring of an
    /// unrelated request and misattribute it.
    /// </summary>
    private const int MinSectionMatchLength = 40;

    /// <summary>The fake key every test launch uses — the stub accepts anything.</summary>
    private const string StubApiKey = "sk-stub-not-a-real-key";

    private static readonly IReadOnlyDictionary<string, string> SectionPromptsByKey = new Dictionary<string, string>
    {
        ["abstract"] = SummaryPrompts.Abstract,
        ["longSummary"] = SummaryPrompts.Structured,
        ["proseSummary"] = SummaryPrompts.Prose,
        ["emailText"] = SummaryPrompts.Email,
        ["taskList"] = SummaryPrompts.Aufgabe,
        ["conversationNote"] = SummaryPrompts.Gespraechsnotiz,
        ["stundenzettelText"] = SummaryPrompts.Stundenzettel,
        ["analogText"] = SummaryPrompts.Analog,
    };

    /// <summary>
    /// Matches an entry row's concatenated title-line text, e.g.
    /// <c>"003   Neubau  —  Offene Aufgaben Kita Sonnenschein"</c> (three WPF <c>Run</c>s: the
    /// zero-padded <c>SequenceNumber</c>, a space, <c>ProjectName</c>, <c>" — "</c>, <c>Title</c>).
    /// Used by <see cref="FindRowText"/> to find the title line among a row's raw-view Text
    /// children — the <c>ListBoxItem</c>'s own UIA Name is useless (it is the ViewModel's class
    /// name, F09), and the row also has a "✓" Text (only when done) and a type/duration meta line,
    /// so the title line cannot be found by a fixed position.
    /// </summary>
    private static readonly Regex EntryRowPattern = new(@"^(?<num>\d+)\s+(?<project>.+?)\s+—\s+(?<title>.+)$", RegexOptions.Compiled);

    /// <summary>
    /// Shared by <see cref="DiscoverFixtureKeys"/> (walking the Fixtures folder) and the stub's
    /// transcription lookup (matching an uploaded mp3's filename) — one regex, one definition of
    /// "this file belongs to fixture D&lt;n&gt;".
    /// </summary>
    private static readonly Regex FixtureFileNamePattern = new(@"^(D\d+)(?:-|_status\.json$)", RegexOptions.Compiled);

    private readonly IReadOnlyDictionary<string, DictationFixture> fixtures;
    private readonly string exePath;
    private readonly string sandboxRoot;
    private readonly bool keepSandbox;
    private readonly string? savedClipboardText;
    private bool keepOnFailure;
    private bool disposed;

    private UiTestContext(
        string exePath,
        JohannSession app,
        OpenAiStubServer stub,
        SandboxLayout sandbox,
        string sandboxRoot,
        bool keepSandbox,
        IReadOnlyDictionary<string, DictationFixture> fixtures,
        string? savedClipboardText)
    {
        this.savedClipboardText = savedClipboardText;
        this.exePath = exePath;
        this.App = app;
        this.Stub = stub;
        this.Sandbox = sandbox;
        this.sandboxRoot = sandboxRoot;
        this.keepSandbox = keepSandbox;
        this.fixtures = fixtures;
    }

    public JohannSession App { get; private set; }

    public OpenAiStubServer Stub { get; }

    public SandboxLayout Sandbox { get; }

    private static string FixturesDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public static UiTestContext Start(
        bool firstRun = false,
        Action<OpenAiStubServer>? stub = null,
        Func<JsonObject, JsonObject>? adjustSettings = null,
        bool teamFile = false,
        bool keepSandbox = false,
        Func<string, string?>? chatOverride = null)
    {
        var exePath = ExeLocator.Find();
        var lastSeenReleaseNotesVersion = firstRun ? null : TryReadAssemblyVersion(exePath);
        var fixtures = LoadFixtures(FixturesDirectory);
        var root = Path.Combine(Path.GetTempPath(), "johann-ui", Guid.NewGuid().ToString("N"));

        // Copy tests overwrite the clipboard of the person whose desktop this runs on; their
        // text comes back in Dispose (best effort — only text, and only if it can be read).
        var savedClipboardText = TryReadClipboardText();

        var stubServer = OpenAiStubServer.Start();

        // Everything from here on can throw (a test's own `stub` callback, sandbox creation, the
        // launch itself) and must not leak the stub's HTTP listener or a half-written sandbox
        // directory into the next test — one try/catch covers the whole sequence instead of
        // guarding each step separately (a review found the previous version left the stub
        // undisposed when `ApplyDefaultStubRules`/`stub?.Invoke` threw).
        try
        {
            ApplyDefaultStubRules(stubServer, fixtures, chatOverride);
            stub?.Invoke(stubServer);

            Func<JsonObject, JsonObject> adjust = json =>
            {
                if (teamFile)
                {
                    var teamPromptsPath = Path.Combine(root, "team", "prompts.json");
                    Directory.CreateDirectory(Path.GetDirectoryName(teamPromptsPath)!);
                    File.WriteAllText(teamPromptsPath, BuiltInTeamPromptsJson());
                    json["globalPromptFilePath"] = teamPromptsPath;
                }

                return adjustSettings is null ? json : adjustSettings(json);
            };

            var sandbox = TestSandbox.Create(root, lastSeenReleaseNotesVersion, adjust);
            var options = new JohannLaunchOptions(exePath, sandbox, stubServer.Root, StubApiKey);
            var app = JohannSession.Launch(options);

            return new UiTestContext(exePath, app, stubServer, sandbox, root, keepSandbox, fixtures, savedClipboardText);
        }
        catch
        {
            stubServer.Dispose();
            if (!keepSandbox)
            {
                // Safe even when TestSandbox.Create never ran (root does not exist yet) or only
                // partially created the sandbox before throwing — TryDeleteDirectory no-ops on a
                // missing directory and swallows a locked one, it never masks the real exception.
                TryDeleteDirectory(root);
            }

            throw;
        }
    }

    /// <summary>
    /// Drops the fixture MP3 (e.g. <c>"D1"</c> for <c>D1-aufgaben.mp3</c>) into the sandbox's
    /// watch folder and waits until a list entry with the fixture's title appears, returning that
    /// title.
    /// </summary>
    public async Task<string> DropDictationAsync(string fixture, TimeSpan? timeout = null)
    {
        if (!this.fixtures.TryGetValue(fixture, out var data))
        {
            throw new InvalidOperationException(
                $"Kein Stub-Fixture für '{fixture}' geladen — MP3/TXT unter '{FixturesDirectory}' erwartet.");
        }

        var sourceMp3 = Directory.EnumerateFiles(FixturesDirectory, $"{fixture}-*.mp3").FirstOrDefault()
            ?? throw new FileNotFoundException($"Fixture-MP3 '{fixture}' nicht gefunden unter '{FixturesDirectory}'.");

        Directory.CreateDirectory(this.Sandbox.Eingang);
        File.Copy(sourceMp3, Path.Combine(this.Sandbox.Eingang, Path.GetFileName(sourceMp3)), overwrite: true);

        var expectedTitle = data.Full?.Title ?? FirstWords(data.Transcript, 5);

        // 60s, not 30s: observed live (Task 11) that the very first launch after a rebuild can be
        // slow enough (e.g. Defender scanning the freshly written exe/dll) to blow a 30s budget on
        // an otherwise healthy run — repeat runs against an unchanged binary took 13-31s.
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(60));
        var lastSeen = new List<string>();
        while (DateTime.UtcNow < deadline)
        {
            // Exact match first (AutomationId or Name equal to the title) — cheap, and correct if
            // a later change gives list rows an explicit AutomationId/Name equal to the title.
            if (this.App.TryFind(expectedTitle) is not null)
            {
                return expectedTitle;
            }

            // Fallback: today's ListBoxItem has no such explicit id: its UIA Name (and/or its
            // descendant Text runs, e.g. the "001 Projekt — Titel" line) merely *contains* the
            // title. Walk the entry list directly and match by Contains instead of exact Name.
            if (this.TryFindTitleInEntryList(expectedTitle, out var seen))
            {
                return expectedTitle;
            }

            lastSeen = seen;
            await Task.Delay(250).ConfigureAwait(false);
        }

        var seenText = lastSeen.Count == 0 ? "(keine Einträge/Texte gesehen)" : string.Join(" | ", lastSeen);
        throw new TimeoutException(
            $"Diktat '{fixture}' erschien nicht als Listeneintrag '{expectedTitle}' innerhalb von {timeout}. "
            + $"Zuletzt gesehene Eintragstexte: {seenText}");
    }

    /// <summary>
    /// Marks this context so <see cref="Dispose"/> preserves a screenshot and the UI tree even
    /// when <c>JOHANN_UI_KEEP</c> is not set — a test calls this from its own failure handling
    /// (xUnit 2 has no built-in "on failure" hook).
    /// </summary>
    public void KeepOnFailure() => this.keepOnFailure = true;

    /// <summary>Titles of every row currently shown in <c>Entries.List</c>, in list order.</summary>
    public IReadOnlyList<string> EntryTitles() => this.EntryRows().Select(r => r.Title).ToList();

    /// <summary>The <c>SequenceNumber</c> (e.g. 1, 2, 3) of every row currently shown, in list order.</summary>
    public IReadOnlyList<int> EntryNumbers() => this.EntryRows().Select(r => r.Number).ToList();

    /// <summary>
    /// Selects the row whose title equals <paramref name="title"/> with a mouse click on its title
    /// text (the <c>ListItem</c> has no Invoke pattern — <see cref="JohannSession.Click"/> already
    /// falls back to a mouse click for that, but only once it has found the row by its exact,
    /// number-prefixed row text).
    /// </summary>
    public void SelectEntry(string title)
    {
        var row = this.EntryRows().FirstOrDefault(r => r.Title == title)
            ?? throw new InvalidOperationException($"Kein Eintrag mit Titel '{title}' in Entries.List gefunden.");
        this.App.Click(row.RowText, mouse: true);
    }

    /// <summary>
    /// The title of whichever row currently has UIA's <c>SelectionItem.IsSelected</c> set, or
    /// <c>null</c> if none does. Uses <c>PatternOrDefault</c> throughout — a row without the
    /// SelectionItem pattern (should not happen for a <c>ListBoxItem</c>, but this must never throw)
    /// is simply skipped.
    /// </summary>
    public string? SelectedEntryTitle()
    {
        var list = this.App.TryFind("Entries.List");
        if (list is null)
        {
            return null;
        }

        foreach (var item in list.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)))
        {
            var selection = item.Patterns.SelectionItem.PatternOrDefault;
            if (selection is null || !selection.IsSelected.ValueOrDefault)
            {
                continue;
            }

            var rowText = FindRowText(item);
            return EntryRowPattern.Match(rowText).Groups["title"].Value;
        }

        return null;
    }

    /// <summary>
    /// The date labels shown in <c>Dates.List</c> (e.g. <c>"24.09."</c>), in list order. The list
    /// is grouped (<c>CollectionViewSource</c>/<c>GroupStyle</c>), so rows are found via
    /// <c>FindAllDescendants</c> rather than <c>FindAllChildren</c> — a direct child of the
    /// <c>List</c> control here is a group container, not a row.
    /// </summary>
    public IReadOnlyList<string> DateItems()
    {
        var list = this.App.TryFind("Dates.List")
            ?? throw new InvalidOperationException("Dates.List nicht gefunden.");

        var result = new List<string>();
        foreach (var item in list.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)))
        {
            var text = item.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text))
                .FirstOrDefault();
            var name = text?.Properties.Name.ValueOrDefault;
            if (!string.IsNullOrEmpty(name))
            {
                result.Add(name);
            }
        }

        return result;
    }

    /// <summary>
    /// Polls <paramref name="condition"/> until it returns <see langword="true"/> or
    /// <paramref name="timeout"/> elapses. Every assertion that follows an async UI action (a
    /// delete, "Als erledigt markieren", a filter toggle) must poll like this rather than reading
    /// state exactly once — MainViewModel's list reconciliation (#100) and any repository-backed
    /// reload are never synchronous with the click that triggers them. <paramref name="condition"/>
    /// may itself throw while state is transiently inconsistent (e.g. a row mid-move); that is
    /// swallowed and retried, and reported as the "last observed state" if the whole wait times out.
    /// </summary>
    public void WaitUntil(Func<bool> condition, TimeSpan timeout, string because)
    {
        var deadline = DateTime.UtcNow + timeout;
        Exception? lastError = null;
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                if (condition())
                {
                    return;
                }

                lastError = null;
            }
            catch (Exception ex)
            {
                lastError = ex;
            }

            Thread.Sleep(100);
        }

        var lastState = lastError is null ? string.Empty : $" Zuletzt beobachtet: {lastError.GetType().Name}: {lastError.Message}";
        throw new TimeoutException($"Zeitüberschreitung ({timeout}) beim Warten auf: {because}.{lastState}");
    }

    /// <summary>
    /// Waits until <c>Detail.Reprocess</c> is enabled again. Only as strong as that button:
    /// <c>ReprocessCommand</c>'s CanExecute is <c>CanReprocess</c>, which no other running command
    /// (a section generation, a transcript regeneration) turns off — a caller must additionally
    /// wait for the concrete end state it expects (Task 12 report).
    /// </summary>
    public Task WaitUntilIdleAsync(TimeSpan? timeout = null) =>
        Task.Run(() => this.WaitUntil(
            () => this.App.Find("Detail.Reprocess", TimeSpan.FromSeconds(1)).IsEnabled,
            timeout ?? TimeSpan.FromSeconds(60),
            "Detail.Reprocess ist wieder aktiviert"));

    /// <summary>
    /// Waits for a file matching <paramref name="pattern"/> anywhere below
    /// <paramref name="directory"/> (recursive) and returns its path. With
    /// <paramref name="changedAfterUtc"/>, only a file written after that instant counts — the
    /// day folder already holds the PDF written during processing, and an export overwrites it
    /// under the same name. The file must also open for reading, so a half-written file is not
    /// returned while its writer still holds it.
    /// </summary>
    public Task<string> WaitForFileAsync(string directory, string pattern, DateTime? changedAfterUtc = null, TimeSpan? timeout = null) =>
        Task.Run(() =>
        {
            string? found = null;
            this.WaitUntil(
                () =>
                {
                    found = Directory.Exists(directory)
                        ? Directory.EnumerateFiles(directory, pattern, SearchOption.AllDirectories)
                            .FirstOrDefault(f => (changedAfterUtc is null || File.GetLastWriteTimeUtc(f) > changedAfterUtc) && CanOpenForRead(f))
                        : null;
                    return found is not null;
                },
                timeout ?? TimeSpan.FromSeconds(30),
                $"Datei '{pattern}' unter '{directory}'" + (changedAfterUtc is null ? string.Empty : $" geschrieben nach {changedAfterUtc:O}"));
            return found!;
        });

    /// <summary>
    /// Closes Johann and starts it again against the same sandbox and stub — what a user does
    /// between two sessions. Everything the first session persisted must be read back from disk.
    /// </summary>
    public void Restart()
    {
        this.App.Dispose();
        this.App = JohannSession.Launch(new JohannLaunchOptions(this.exePath, this.Sandbox, this.Stub.Root, StubApiKey));
    }

    /// <summary>
    /// Copies the fixture MP3 (e.g. <c>"D1"</c>) into the watch folder without waiting for an
    /// entry — for flows where processing is expected to fail.
    /// </summary>
    public void DropFile(string fixture)
    {
        var sourceMp3 = Directory.EnumerateFiles(FixturesDirectory, $"{fixture}-*.mp3").FirstOrDefault()
            ?? throw new FileNotFoundException($"Fixture-MP3 '{fixture}' nicht gefunden unter '{FixturesDirectory}'.");

        Directory.CreateDirectory(this.Sandbox.Eingang);
        File.Copy(sourceMp3, Path.Combine(this.Sandbox.Eingang, Path.GetFileName(sourceMp3)), overwrite: true);
    }

    /// <summary>
    /// Opens the settings window unless it is already open, then selects the left-nav section
    /// <c>Settings.Section.&lt;key&gt;</c> (lower-case keys from <c>SettingsViewModel</c>, e.g.
    /// <c>"kategorien"</c> for „Vorlagen“, <c>"ki-modell"</c>).
    /// </summary>
    public void OpenSettingsSection(string key)
    {
        if (this.App.TryFind("Settings.Save", TimeSpan.FromMilliseconds(300)) is null)
        {
            this.App.Click("Main.Settings", mouse: true);
        }

        // Selected through UIA's SelectionItem pattern, not a mouse click: the settings window
        // may open on a monitor with negative coordinates, where a click can miss (known driver
        // issue, Task 12 report).
        var section = this.App.Find($"Settings.Section.{key}");
        section.Patterns.SelectionItem.Pattern.Select();
        this.WaitUntil(
            () => this.App.Find($"Settings.Section.{key}").Patterns.SelectionItem.Pattern.IsSelected.ValueOrDefault,
            TimeSpan.FromSeconds(5),
            $"Settings.Section.{key} ist ausgewählt");
    }

    /// <summary>
    /// Picks the summary model <paramref name="modelId"/> in <c>Settings.ModelPicker</c>: the
    /// item whose name or text contains the model's display name (the picker shows
    /// <c>DisplayName</c>, never the raw id).
    /// </summary>
    public void SelectModel(string modelId)
    {
        var displayName = SummaryModelCatalog.TryFind(modelId)?.DisplayName
            ?? throw new InvalidOperationException($"Modell '{modelId}' steht nicht im SummaryModelCatalog.");
        this.SelectComboItem("Settings.ModelPicker", item => ElementTexts(item).Any(t => t.Contains(displayName, StringComparison.Ordinal)), displayName);
    }

    /// <summary>
    /// Selects an item of the combo box <paramref name="comboId"/> through UIA's SelectionItem
    /// pattern (expand, select, collapse) instead of mouse clicks into the drop-down popup — the
    /// popup is its own untitled window, which the foreground/click-point safety checks of
    /// <see cref="JohannSession.Click"/> are not built for.
    /// </summary>
    public void SelectComboItem(string comboId, Func<AutomationElement, bool> match, string description)
    {
        var combo = this.App.Find(comboId).AsComboBox();
        combo.Expand();
        try
        {
            var item = combo.Items.FirstOrDefault(i => match(i))
                ?? throw new InvalidOperationException(
                    $"Kein Eintrag '{description}' in {comboId} — vorhanden: [{string.Join(" | ", combo.Items.Select(i => string.Join("/", ElementTexts(i))))}]");
            item.Select();
        }
        finally
        {
            combo.Collapse();
        }
    }

    /// <summary>
    /// Waits for a <c>Toast.Item</c> that has finished (no running progress bar) and whose text
    /// satisfies <paramref name="accept"/> (default: any), returning its text — title and message
    /// joined by a line break.
    /// </summary>
    public Task<string> WaitForToastAsync(Func<string, bool>? accept = null, TimeSpan? timeout = null) =>
        Task.Run(() =>
        {
            string? found = null;
            var seen = new List<string>();
            try
            {
                this.WaitUntil(
                    () =>
                    {
                        seen = this.FinishedToastTexts();
                        found = seen.FirstOrDefault(t => accept?.Invoke(t) ?? true);
                        return found is not null;
                    },
                    timeout ?? TimeSpan.FromSeconds(60),
                    "eine abgeschlossene Meldung (Toast.Item)");
            }
            catch (TimeoutException ex)
            {
                throw new TimeoutException($"{ex.Message} Gesehene Meldungen: [{string.Join(" | ", seen)}]", ex);
            }

            return found!;
        });

    /// <summary>
    /// Presses Tab up to <paramref name="maxSteps"/> times in the main window and returns the
    /// AutomationId of every element that received focus, in the order first reached.
    /// </summary>
    public IReadOnlyList<string> TabThroughWindow(int maxSteps)
    {
        var reached = new List<string>();
        for (var step = 0; step < maxSteps; step++)
        {
            this.App.Key("Tab");
            Thread.Sleep(60);
            var id = this.App.FocusedAutomationId();
            if (!string.IsNullOrEmpty(id) && !reached.Contains(id, StringComparer.Ordinal))
            {
                reached.Add(id);
            }
        }

        return reached;
    }

    public void Dispose()
    {
        if (this.disposed)
        {
            return;
        }

        this.disposed = true;

        if (this.keepOnFailure || Environment.GetEnvironmentVariable("JOHANN_UI_KEEP") == "1")
        {
            this.TrySaveArtifacts();
        }

        this.App.Dispose();
        this.Stub.Dispose();
        TryRestoreClipboardText(this.savedClipboardText);

        if (!this.keepSandbox)
        {
            TryDeleteDirectory(this.sandboxRoot);
        }
    }

    /// <summary>
    /// Finds the row's title-line text among its Text children, read via the UIA <b>raw view</b>
    /// rather than the control view. Found live (fix round 1, #111): after
    /// <c>MainViewModel.ArrangeRows</c> repositions an existing row via
    /// <c>ObservableCollection.Move(...)</c> (the #100 reconcile-instead-of-rebuild optimisation),
    /// WPF's automation peer leaves that row's OLD child peers behind as
    /// <c>IsControlElement=False</c>, zero-bounds ghosts. The control view
    /// (<c>FindAllDescendants</c>, even with a raw/true condition) then reports zero children for
    /// that row — but PrintWindow screenshots confirmed the row is drawn correctly on screen the
    /// whole time, and the raw view still walks straight to the real children, whose
    /// <c>Name</c> is the live, correct text. This is a UIA peer bug, not a rendering bug — reading
    /// through the raw view sidesteps it instead of waiting out something that never resolves.
    /// <para>
    /// Throws (rather than silently skipping the row) when none of a <c>ListItem</c>'s raw-view
    /// Text children match <see cref="EntryRowPattern"/> — a row that genuinely has no title line
    /// is a real problem a caller must see, not a row quietly missing from the result.
    /// </para>
    /// </summary>
    private static string FindRowText(AutomationElement item)
    {
        var texts = RawViewChildTexts(item);
        var match = texts.FirstOrDefault(t => EntryRowPattern.IsMatch(t));
        if (match is not null)
        {
            return match;
        }

        throw new InvalidOperationException(
            $"Zeile ohne Titeltext (Raw-View durchsucht) — gefundene Texte: [{string.Join(" | ", texts)}]");
    }

    /// <summary>
    /// Walks <paramref name="item"/>'s children via the UIA raw view
    /// (<c>TreeWalkerFactory.GetRawViewWalker()</c>) and returns every Text child's <c>Name</c>.
    /// See <see cref="FindRowText"/> for why the raw view is used instead of
    /// <c>FindAllDescendants</c>.
    /// </summary>
    private static IReadOnlyList<string> RawViewChildTexts(AutomationElement item)
    {
        var walker = item.Automation.TreeWalkerFactory.GetRawViewWalker();
        var texts = new List<string>();
        for (var child = walker.GetFirstChild(item); child is not null; child = walker.GetNextSibling(child))
        {
            if (child.Properties.ControlType.ValueOrDefault != FlaUI.Core.Definitions.ControlType.Text)
            {
                continue;
            }

            var name = child.Properties.Name.ValueOrDefault;
            if (!string.IsNullOrEmpty(name))
            {
                texts.Add(name);
            }
        }

        return texts;
    }

    /// <summary>
    /// The element's own name plus the name of every Text descendant — a combo box item bound
    /// to a record reports the record's <c>ToString()</c> as its name, and its visible text only
    /// as a child.
    /// </summary>
    private static IReadOnlyList<string> ElementTexts(AutomationElement element)
    {
        var texts = new List<string>();
        var name = element.Properties.Name.ValueOrDefault;
        if (!string.IsNullOrEmpty(name))
        {
            texts.Add(name);
        }

        foreach (var text in element.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text)))
        {
            var value = text.Properties.Name.ValueOrDefault;
            if (!string.IsNullOrEmpty(value))
            {
                texts.Add(value);
            }
        }

        return texts;
    }

    private static void ApplyDefaultStubRules(
        OpenAiStubServer stub,
        IReadOnlyDictionary<string, DictationFixture> fixtures,
        Func<string, string?>? chatOverride)
    {
        stub.OnTranscription(fileName =>
        {
            var key = ExtractFixtureKey(fileName);
            return key is not null && fixtures.TryGetValue(key, out var data) ? data.Transcript : null;
        });

        var headerParser = new HeaderParser();

        stub.OnChat(userContent =>
        {
            // A test's own answer wins, and everything it does not answer still gets the fixture
            // defaults — unlike a `stub` callback's OnChat, which replaces the default responder
            // outright (Task 12: a regenerated, edited transcript matches no fixture).
            if (chatOverride?.Invoke(userContent) is { } overridden)
            {
                return overridden;
            }

            // Three passes across ALL fixtures, not one combined per-fixture OR condition: a title
            // request embeds header.RemainderText (EntryProcessingService.ProcessAudioAsync) — the
            // transcript with the leading "Projekt <Name>." (or other type/project) tokens already
            // stripped by HeaderParser, NOT the full transcript. EmailText (and any future chained
            // section) embeds an earlier section's OWN output instead of the transcript at all
            // (SummaryGenerator.GenerateEmailTextAsync embeds {prose_summary} verbatim). Matching
            // only on the full transcript made every title request AND the email request miss (real
            // bug, found live, Task 11: both 500'd on every single dictation and aborted processing
            // before an entry was ever saved). Passes, in order of specificity, avoid a short
            // section value coincidentally matching before the transcript itself is tried.
            var match = fixtures.Values.FirstOrDefault(f =>
                userContent.Contains(f.Transcript, StringComparison.Ordinal));

            match ??= fixtures.Values.FirstOrDefault(f =>
            {
                var remainder = headerParser.Parse(f.Transcript).RemainderText;
                return !string.IsNullOrEmpty(remainder) && userContent.Contains(remainder, StringComparison.Ordinal);
            });

            match ??= fixtures.Values.FirstOrDefault(f =>
                f.Full is not null && f.Full.Sections.Values.Any(v =>
                    !string.IsNullOrEmpty(v)
                    && v.Length >= MinSectionMatchLength
                    && userContent.Contains(v, StringComparison.Ordinal)));

            if (match is null)
            {
                return null;
            }

            if (SectionPromptMatcher.IsTitleRequest(userContent))
            {
                // The fixture's own title (from D<n>_status.json) when it has one — DropDictationAsync
                // waits for exactly that title, and it is normally a short semantic title an LLM would
                // produce, not literally the transcript's first five words.
                return match.Full?.Title ?? FirstWords(match.Transcript, 5);
            }

            var sectionKey = SectionPromptMatcher.MatchSection(userContent, SectionPromptsByKey);
            if (sectionKey is null)
            {
                return null;
            }

            if (match.Full is not null && match.Full.Sections.TryGetValue(sectionKey, out var text))
            {
                return text;
            }

            return $"**Test** {sectionKey}\n- Punkt 1\n  - Unterpunkt";
        });
    }

    private static IReadOnlyDictionary<string, DictationFixture> LoadFixtures(string fixturesDirectory)
    {
        var result = new Dictionary<string, DictationFixture>(StringComparer.Ordinal);
        if (!Directory.Exists(fixturesDirectory))
        {
            return result;
        }

        // Discovered from whatever is actually on disk (D1, D2, … as far as fixture files exist)
        // rather than a hard-coded count — adding a D7 fixture later needs no code change here.
        foreach (var key in DiscoverFixtureKeys(fixturesDirectory))
        {
            var statusPath = Path.Combine(fixturesDirectory, $"{key}_status.json");
            if (File.Exists(statusPath))
            {
                var full = EntryFixture.Load(statusPath);
                result[key] = new DictationFixture(full.Transcript, full);
                continue;
            }

            var txtPath = Directory.EnumerateFiles(fixturesDirectory, $"{key}-*.txt").FirstOrDefault();
            if (txtPath is not null)
            {
                result[key] = new DictationFixture(File.ReadAllText(txtPath).Trim(), null);
            }
        }

        return result;
    }

    /// <summary>
    /// Finds every fixture key (e.g. <c>"D1"</c>, <c>"D12"</c>) present in
    /// <paramref name="fixturesDirectory"/>, recognised from either a <c>D&lt;n&gt;-*.mp3</c>/
    /// <c>D&lt;n&gt;-*.txt</c> pair or a <c>D&lt;n&gt;_status.json</c> file — via the same
    /// <see cref="ExtractFixtureKey"/> the stub's transcription lookup uses, so the two never
    /// disagree on what counts as a fixture file.
    /// </summary>
    private static IReadOnlyCollection<string> DiscoverFixtureKeys(string fixturesDirectory)
    {
        var keys = new SortedSet<string>(StringComparer.Ordinal);
        foreach (var file in Directory.EnumerateFiles(fixturesDirectory))
        {
            var key = ExtractFixtureKey(file);
            if (key is not null)
            {
                keys.Add(key);
            }
        }

        return keys;
    }

    private static string? ExtractFixtureKey(string fileName)
    {
        var match = FixtureFileNamePattern.Match(Path.GetFileName(fileName));
        return match.Success ? match.Groups[1].Value : null;
    }

    private static string FirstWords(string text, int count) =>
        string.Join(' ', text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Take(count));

    private static string? TryReadAssemblyVersion(string exePath)
    {
        try
        {
            // exePath is the native apphost stub (Platee.Johann.UI.exe) — it has no managed PE
            // metadata, so AssemblyName.GetAssemblyName on it always threw BadImageFormatException
            // (silently swallowed below), meaning lastSeenReleaseNotesVersion was NEVER written and
            // every Start() showed the release-notes modal (found live, Task 11 — it blocked
            // DropDictationAsync on every test that used the default Start()). The managed assembly
            // sits right next to it as the .dll with the same base name; that one has real metadata.
            var managedAssemblyPath = Path.ChangeExtension(exePath, ".dll");
            return AssemblyName.GetAssemblyName(managedAssemblyPath).Version?.ToString(3);
        }
        catch (Exception)
        {
            // Best-effort: worst case a normal Start() shows the release-notes window once,
            // which the relevant smoke test (FirstRun_ShowsReleaseNotes) already covers.
            return null;
        }
    }

    private static string BuiltInTeamPromptsJson()
    {
        var json = new JsonObject
        {
            ["systemMessage"] = SummaryPrompts.SystemMessage,
            ["abstractPrompt"] = SummaryPrompts.Abstract,
            ["structuredPrompt"] = SummaryPrompts.Structured,
            ["prosePrompt"] = SummaryPrompts.Prose,
            ["emailPrompt"] = SummaryPrompts.Email,
            ["aufgabePrompt"] = SummaryPrompts.Aufgabe,
            ["gespraechsnotizPrompt"] = SummaryPrompts.Gespraechsnotiz,
            ["stundenzettelPrompt"] = SummaryPrompts.Stundenzettel,
            ["analogPrompt"] = SummaryPrompts.Analog,
        };

        return json.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
    }

    private static string? TryReadClipboardText()
    {
        string? text = null;
        TryClipboard(() => text = JohannSession.ReadClipboard());
        return string.IsNullOrEmpty(text) ? null : text;
    }

    private static void TryRestoreClipboardText(string? text)
    {
        if (text is not null)
        {
            TryClipboard(() => JohannSession.WriteClipboard(text));
        }
    }

    /// <summary>
    /// Best effort with a few short retries: the clipboard is briefly held by whichever process
    /// last wrote it (CLIPBRD_E_CANT_OPEN), and a failure here must never fail a test.
    /// </summary>
    private static void TryClipboard(Action action)
    {
        for (var attempt = 0; attempt < 5; attempt++)
        {
            try
            {
                action();
                return;
            }
            catch (Exception)
            {
                Thread.Sleep(100);
            }
        }
    }

    private static bool CanOpenForRead(string path)
    {
        try
        {
            using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
            return stream.Length > 0;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private static void TryDeleteDirectory(string path)
    {
        try
        {
            if (Directory.Exists(path))
            {
                Directory.Delete(path, recursive: true);
            }
        }
        catch (Exception)
        {
            // Best-effort cleanup — a locked file (e.g. AV scan) must not fail the test itself.
        }
    }

    /// <summary>
    /// Looks for <paramref name="expectedTitle"/> as a substring of the entry list's row names or
    /// their descendant text runs. <paramref name="seenNames"/> always carries every name observed
    /// (even on a miss), so a timeout can report what was actually on screen.
    /// </summary>
    private bool TryFindTitleInEntryList(string expectedTitle, out List<string> seenNames)
    {
        seenNames = [];

        var list = this.App.TryFind("Entries.List");
        if (list is null)
        {
            return false;
        }

        foreach (var item in list.FindAllChildren())
        {
            var itemName = item.Properties.Name.ValueOrDefault;
            if (!string.IsNullOrEmpty(itemName))
            {
                seenNames.Add(itemName);
                if (itemName.Contains(expectedTitle, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            foreach (var descendant in item.FindAllDescendants())
            {
                if (descendant.Properties.ControlType.ValueOrDefault != FlaUI.Core.Definitions.ControlType.Text)
                {
                    continue;
                }

                var text = descendant.Properties.Name.ValueOrDefault;
                if (string.IsNullOrEmpty(text))
                {
                    continue;
                }

                seenNames.Add(text);
                if (text.Contains(expectedTitle, StringComparison.Ordinal))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Text of every shown <c>Toast.Item</c> without a visible progress bar (a running toast
    /// shows one until its job completes), each as its direct Text children (title, message)
    /// joined by a line break.
    /// </summary>
    private List<string> FinishedToastTexts()
    {
        var result = new List<string>();
        foreach (var toast in this.App.MainWindow.FindAllDescendants(cf => cf.ByAutomationId("Toast.Item")))
        {
            if (toast.FindFirstDescendant(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.ProgressBar)) is not null)
            {
                continue;
            }

            // Direct Text children only: the close button's "×" and the details link carry
            // Text descendants of their own, and "×" would come first.
            var texts = toast.FindAllChildren(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text))
                .Select(t => t.Properties.Name.ValueOrDefault)
                .Where(t => !string.IsNullOrEmpty(t));
            result.Add(string.Join("\n", texts));
        }

        return result;
    }

    private IReadOnlyList<EntryRow> EntryRows()
    {
        var list = this.App.TryFind("Entries.List");
        if (list is null)
        {
            return [];
        }

        var rows = new List<EntryRow>();
        foreach (var item in list.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem)))
        {
            var rowText = FindRowText(item);
            var match = EntryRowPattern.Match(rowText);
            rows.Add(new EntryRow(rowText, int.Parse(match.Groups["num"].Value), match.Groups["title"].Value));
        }

        return rows;
    }

    private void TrySaveArtifacts()
    {
        try
        {
            var directory = Path.Combine("TestResults", "ui", $"{DateTime.UtcNow:yyyyMMdd-HHmmssfff}-{Guid.NewGuid():N}");
            Directory.CreateDirectory(directory);
            this.App.Screenshot(Path.Combine(directory, "screenshot.png"));
            File.WriteAllText(Path.Combine(directory, "tree.json"), this.App.Tree());
        }
        catch (Exception)
        {
            // Diagnostics are best-effort — losing them must not hide the real test failure.
        }
    }

    /// <summary>See <see cref="EntryRowPattern"/> — one entry row's parsed title line.</summary>
    private sealed record EntryRow(string RowText, int Number, string Title);

    private sealed record DictationFixture(string Transcript, EntryFixture? Full);
}
