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
    /// <c>"003 Neubau — Offene Aufgaben Kita Sonnenschein"</c> (three WPF <c>Run</c>s: the
    /// zero-padded <c>SequenceNumber</c>, a space, <c>ProjectName</c>, <c>" — "</c>, <c>Title</c>).
    /// Used by <see cref="EntryRows"/>/<see cref="FindRowText"/> to find the title line among a
    /// row's Text descendants — the <c>ListBoxItem</c>'s own UIA Name is useless (it is the
    /// ViewModel's class name, F09), and the row also has a "✓" Text (only when done) and a
    /// type/duration meta line, so the title line cannot be found by a fixed position.
    /// </summary>
    private static readonly Regex EntryRowPattern = new(@"^(?<num>\d+)\s+(?<project>.+?)\s+—\s+(?<title>.+)$", RegexOptions.Compiled);

    private readonly IReadOnlyDictionary<string, DictationFixture> fixtures;
    private readonly string sandboxRoot;
    private readonly bool keepSandbox;
    private bool keepOnFailure;
    private bool disposed;

    private UiTestContext(
        JohannSession app,
        OpenAiStubServer stub,
        SandboxLayout sandbox,
        string sandboxRoot,
        bool keepSandbox,
        IReadOnlyDictionary<string, DictationFixture> fixtures)
    {
        this.App = app;
        this.Stub = stub;
        this.Sandbox = sandbox;
        this.sandboxRoot = sandboxRoot;
        this.keepSandbox = keepSandbox;
        this.fixtures = fixtures;
    }

    public JohannSession App { get; }

    public OpenAiStubServer Stub { get; }

    public SandboxLayout Sandbox { get; }

    private static string FixturesDirectory => Path.Combine(AppContext.BaseDirectory, "Fixtures");

    public static UiTestContext Start(
        bool firstRun = false,
        Action<OpenAiStubServer>? stub = null,
        Func<JsonObject, JsonObject>? adjustSettings = null,
        bool teamFile = false,
        bool keepSandbox = false)
    {
        var exePath = ExeLocator.Find();
        var lastSeenReleaseNotesVersion = firstRun ? null : TryReadAssemblyVersion(exePath);
        var fixtures = LoadFixtures(FixturesDirectory);
        var root = Path.Combine(Path.GetTempPath(), "johann-ui", Guid.NewGuid().ToString("N"));

        var stubServer = OpenAiStubServer.Start();

        // Everything from here on can throw (a test's own `stub` callback, sandbox creation, the
        // launch itself) and must not leak the stub's HTTP listener or a half-written sandbox
        // directory into the next test — one try/catch covers the whole sequence instead of
        // guarding each step separately (a review found the previous version left the stub
        // undisposed when `ApplyDefaultStubRules`/`stub?.Invoke` threw).
        try
        {
            ApplyDefaultStubRules(stubServer, fixtures);
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
            var options = new JohannLaunchOptions(exePath, sandbox, stubServer.Root, "sk-stub-not-a-real-key");
            var app = JohannSession.Launch(options);

            return new UiTestContext(app, stubServer, sandbox, root, keepSandbox, fixtures);
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

    /// <summary>See <see cref="EntryRowPattern"/> — one entry row's parsed title line.</summary>
    private sealed record EntryRow(string RowText, int Number, string Title);

    private IReadOnlyList<EntryRow> EntryRows()
    {
        var list = this.App.TryFind("Entries.List");
        if (list is null)
        {
            return [];
        }

        // Retries briefly when a ListItem exists but its Text children are not there yet — found
        // live (Task 11) right after a sort click: WPF can report the container in the UIA tree a
        // layout pass before its DataTemplate's Text runs are populated, so a row is momentarily
        // "present but empty". Real WPF timing, not a parsing bug — a short poll is the fix, not a
        // longer one-shot wait, since the normal case (nothing pending) must stay fast.
        for (var attempt = 0; attempt < 5; attempt++)
        {
            var items = list.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.ListItem));
            var rows = new List<EntryRow>();
            var allPopulated = true;
            foreach (var item in items)
            {
                var rowText = FindRowText(item);
                if (rowText is null)
                {
                    allPopulated = false;
                    continue;
                }

                var match = EntryRowPattern.Match(rowText);
                if (match.Success)
                {
                    rows.Add(new EntryRow(rowText, int.Parse(match.Groups["num"].Value), match.Groups["title"].Value));
                }
            }

            if (allPopulated || attempt == 4)
            {
                return rows;
            }

            Thread.Sleep(200);
        }

        return [];
    }

    private static string? FindRowText(AutomationElement item)
    {
        foreach (var descendant in item.FindAllDescendants(cf => cf.ByControlType(FlaUI.Core.Definitions.ControlType.Text)))
        {
            var name = descendant.Properties.Name.ValueOrDefault;
            if (!string.IsNullOrEmpty(name) && EntryRowPattern.IsMatch(name))
            {
                return name;
            }
        }

        return null;
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
            if (rowText is not null && EntryRowPattern.Match(rowText) is { Success: true } match)
            {
                return match.Groups["title"].Value;
            }
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
        var list = this.App.TryFind("Dates.List");
        if (list is null)
        {
            return [];
        }

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

        if (!this.keepSandbox)
        {
            TryDeleteDirectory(this.sandboxRoot);
        }
    }

    private static void ApplyDefaultStubRules(OpenAiStubServer stub, IReadOnlyDictionary<string, DictationFixture> fixtures)
    {
        stub.OnTranscription(fileName =>
        {
            var key = ExtractFixtureKey(fileName);
            return key is not null && fixtures.TryGetValue(key, out var data) ? data.Transcript : null;
        });

        var headerParser = new HeaderParser();

        stub.OnChat(userContent =>
        {
            // A title request embeds header.RemainderText (EntryProcessingService.ProcessAudioAsync),
            // i.e. the transcript with the leading "Projekt <Name>." (or other type/project) tokens
            // already stripped by HeaderParser — NOT the full transcript. Most section requests embed
            // the full transcript, but EmailText is chained on an earlier section's OWN output
            // (SummaryGenerator.GenerateEmailTextAsync embeds {prose_summary} verbatim, not the
            // transcript) — same shape for any future chained section. Matching only on the
            // transcript made every title request AND the email request miss (real bug, found live,
            // Task 11: both 500'd on every single dictation and aborted processing before an entry
            // was ever saved), so the transcript, its header-stripped remainder, and every one of the
            // fixture's own already-defined section outputs are all tried here.
            var match = fixtures.Values.FirstOrDefault(f =>
                userContent.Contains(f.Transcript, StringComparison.Ordinal)
                || userContent.Contains(headerParser.Parse(f.Transcript).RemainderText, StringComparison.Ordinal)
                || (f.Full is not null && f.Full.Sections.Values.Any(v =>
                    !string.IsNullOrEmpty(v) && userContent.Contains(v, StringComparison.Ordinal))));
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

    /// <summary>
    /// Shared by <see cref="DiscoverFixtureKeys"/> (walking the Fixtures folder) and the stub's
    /// transcription lookup (matching an uploaded mp3's filename) — one regex, one definition of
    /// "this file belongs to fixture D&lt;n&gt;".
    /// </summary>
    private static readonly Regex FixtureFileNamePattern = new(@"^(D\d+)(?:-|_status\.json$)", RegexOptions.Compiled);

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

    private sealed record DictationFixture(string Transcript, EntryFixture? Full);
}
