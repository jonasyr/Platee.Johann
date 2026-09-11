# Platé.Johann

<!-- AUTO-MANAGED: project-description -->
## Overview

**Platé.Johann** is an AI-powered dictation-to-journal tool for Windows. Users drop an MP3 recorded on their smartphone into a watch folder; Johann automatically transcribes it via OpenAI Whisper, generates structured summaries with GPT, and archives the result as HTML and PDF.

Key features:
- Automatic MP3 watch-folder processing (FileSystemWatcher)
- In-app microphone dictation ("🎙 Diktieren" button, WASAPI recording via NAudio)
- OpenAI Whisper transcription + GPT summarisation
- Inline transcript editing with regeneration from corrected text
- Five entry types: Aufgabe, E-Mail, Gesprächsnotiz, Stundenzettel, Analog
- User-definable categories (personal + team) with per-section Auto / on-demand generation
- WPF three-pane UI: date list → entry list → detail view
- Velopack-based installer with GitHub Releases auto-update
- Fully offline/viewer mode when no API key is present

<!-- END AUTO-MANAGED -->

<!-- AUTO-MANAGED: build-commands -->
## Build & Development Commands

```powershell
# Build entire solution
dotnet build

# Run all tests
dotnet test

# Run the application
dotnet run --project Platee.Johann.UI

# Build installer (requires vpk CLI tool installed globally)
.\build-installer.ps1 -Version 1.x.x

# Install vpk tool (once)
dotnet tool install -g vpk
```

Version: **1.4.0**

Test framework: **xUnit 2.9** · Mocking: **NSubstitute 5.3** · Assertions: **FluentAssertions 8.8**
Target: **.NET 10 / net10.0-windows** (UI), **net10.0** (all other projects)

<!-- END AUTO-MANAGED -->

<!-- AUTO-MANAGED: architecture -->
## Architecture

Clean Architecture with four projects + one test project:

```
Platee.Johann.Domain/          # Core entities, no external deps
  Entities/Entry.cs            # Immutable sealed record — central domain model
                               #   EditedTranscript + EffectiveTranscript (edited ?? original)
                               #   CustomSections + CustomSectionNames (schema v4)
  Enums/EntryType.cs
  Parsing/                     # Header, title, type extraction from filenames
  Services/                    # DurationFormatter (shared formatting helper)
  ValueObjects/                # ParsedHeader, ProcessingStatus, CorrectionEntry

Platee.Johann.Application/     # Use-cases, interfaces (depends on Domain only)
  Interfaces/                  # IEntryRepository (incl. MigrateJobIdsAsync),
                               #   ILlmProvider, IAudioTranscriber, IPromptSettingsRepository,
                               #   IMicrophoneRecorder, IModelAvailabilityProbe
  Processing/                  # EntryProcessingService, SummaryGenerator, AudioWatcherService,
                               #   SectionCatalog (SectionDescriptor), ModelNames,
                               #   SummaryModelCatalog, SummaryModelResolver,
                               #   DictationCostEstimator
  Services/                    # PromptSettingsLoader (local/global fallback)
  Settings/                    # AppSettings, PromptSettings, SettingsHolder,
                               #   SettingsSplitMigration,
                               #   CategoryDefinition, BuiltInSections, CategoryIdFactory,
                               #   SectionModeDefaults, SectionModeMigration

Platee.Johann.Infrastructure/  # Concrete adapters (depends on Application + Domain)
  Audio/                       # WindowsMicrophoneRecorder (NAudio 2.2.1 WasapiCapture → temp WAV → MP3),
                               #   NoOpMicrophoneRecorder stub, AudioDurationReader
  Json/                        # JsonRepository (file-backed), JsonSettingsRepository,
                               #   JsonPromptSettingsRepository, migration
  Llm/                         # OpenAiLlmProvider (ChatClient per model id),
                               #   WhisperTranscriber (gpt-transcribe), ApiKeyProvider,
                               #   OpenAiModelAvailabilityProbe, NoOp stubs
  Renderers/                   # HtmlRenderer, PdfRenderer, EmailRenderer, HtmlOverviewService

Platee.Johann.UI/              # WPF presentation layer (depends on all)
  Assets/                      # RELEASE_NOTES.md, HANDBUCH.html (embedded resources,
                               #   auto-copied from repo root via CopyDocsToAssets MSBuild target)
  Helpers/                     # DurationFormatter, ReleaseNotesHelper — pure static helpers
  ViewModels/                  # MainViewModel, SettingsViewModel, NewEntryViewModel,
                               #   CorrectionEntryViewModel, CategoryEditorViewModel,
                               #   SectionRowViewModel, SectionVisibilityViewModel,
                               #   CustomSectionToggleViewModel, …
                               #   Toast stack: ToastTone, ToastToneHelper, ToastItem,
                               #                ToastQueue, ToastsViewModel
  Views/                       # NewEntryView.xaml, ReleaseNotesWindow.xaml,
                               #   SectionModeMigrationDialog.xaml, SettingsView.xaml,
                               #   ToastView.xaml
                               #   (AdminPasswordDialog deleted in v1.3.3)
  Converters/                  # WPF value converters
  Program.cs                   # Entry point + Velopack init + crash logging

Platee.Johann.Tests/
  Unit/                        # xUnit unit tests mirroring all layers
```

Dependency flow: `UI → Infrastructure → Application → Domain`

Data flow: MP3 file → `AudioWatcherService` → `EntryProcessingService` → `SummaryGenerator` (LLM) → `IEntryRepository.Save()` → UI refresh

<!-- END AUTO-MANAGED -->

<!-- AUTO-MANAGED: conventions -->
## Code Conventions

**Immutability**: Domain entities and settings are `sealed record` with `init`-only properties. All mutations produce a new instance (`with` expressions). Never mutate in-place.

**Nullability**: `<Nullable>enable</Nullable>` across all projects. Use `string?` explicitly; avoid `!` suppression.

**Naming**:
- PascalCase for all public members, types, namespaces
- File-scoped namespaces (`namespace Platee.Johann.Domain.Entities;`)
- German field names for user-facing settings (e.g. `Quellverzeichnis`, `Ausgabeverzeichnis`)

**Dependency injection**: All cross-layer dependencies go through interfaces in `Application/Interfaces/`. Infrastructure implements; UI wires up via manual DI in `App.xaml.cs`.

**Imports**: `ImplicitUsings` enabled. Add explicit usings only when not covered by implicit set.

**Tests**: Named `<SubjectUnderTest>Tests.cs`, located in `Platee.Johann.Tests/Unit/`. Use NSubstitute for mocks, FluentAssertions for assertions. ViewModels shared via `<Compile Include=... Link=.../>` in the test project.

<!-- END AUTO-MANAGED -->

<!-- AUTO-MANAGED: patterns -->
## Detected Patterns

**Models (v1.5.0, #71)**: `ModelNames` holds the transcription id (`gpt-transcribe`);
`SummaryModelCatalog` (Application/Processing/) owns the **three** summary models the user may
pick from — `gpt-5.6-luna` (default), `gpt-5.6-terra`, `gpt-5.6-sol`. Choosing a model is an
application decision, calling the SDK with it is infrastructure, so both live in Application.
The chosen id travels per call in `LlmOptions.Model`; `OpenAiLlmProvider` caches one `ChatClient`
per id, because `ChatClient` binds the model in its constructor. `SummaryGenerator.Options()` is
the single place that injects it, so `WithSnapshot()` freezes the model per run for free.

⚠ **Per token ≠ per dictation.** `gpt-5-nano` was in the catalog as "the cheap option" and was
removed on 2026-09-11 after measurement: it burns 2 496 reasoning tokens to produce 514 visible
ones and therefore costs **more per dictation than Luna**, at lower quality. `gpt-5-mini` (59 %
reasoning) and `gpt-5.4-mini` (3.5× Luna) are dominated too — hence three models, not four.

Each catalog entry carries two **measured** constants (`OutputBase`, `OutputSlope`) from which
`DictationCostEstimator` computes the cost shown in the settings card. Everything else is counted
locally, which is why **custom categories need no special handling** — their prompt text is right
there in `prompts.json`. ⚠ The constants were measured **without** a `reasoning_effort`; setting
one (#73) invalidates them.

**Audio duration is measured locally**: `whisper-1` reported it in its Verbose response;
`gpt-transcribe` answers with plain `json` and carries neither duration nor timestamps.
`AudioDurationReader` (Infrastructure/Audio/) reads it from the file via NAudio, falls back to
MediaFoundation for containers `Mp3FileReader` rejects, and **never throws** — a duration is
decoration next to an entry and in the PDF header; losing it must not cost a transcribed
dictation. Validated against 20 archived recordings including a 5:17 one: largest deviation from
the value Whisper had reported was 0.009 s. Only `ProcessAudioAsync` writes `DurationSeconds`,
so reprocessing an old entry preserves it.

**No forced transcription language**: `WhisperTranscriber.ForcedLanguage` is `null`. While it was
pinned to `"de"`, a non-German dictation could not be transcribed at all. The model detects the
language itself; the system prompt keeps the *output* German. The transcript deliberately stays
in the spoken language — it is the record of what was said.

**Markdown rendering**: `MarkdownFlowDocumentConverter` (UI/Converters/) renders every generated
section; only the transcript stays raw, because it is the literal transcription and must stay
editable. It keeps per-bullet indentation and compares indents relatively, so two- and four-space
markdown both nest — until v1.4.0 it detected bullets on the trimmed line and flattened every
outline into one level. That only became visible with a model strong enough to nest.

⚠ **Prompts must not name their own section.** The app already renders the heading; a prompt that
tells the model to "create a Gesprächsnotiz" gets one titled that way, and it then appears twice
in the detail view, the PDF and the mail. Every section prompt now says so explicitly.

**Repository pattern**: `IEntryRepository` / `ISettingsRepository` / `IPromptSettingsRepository` interfaces in Application; `JsonRepository` / `JsonSettingsRepository` / `JsonPromptSettingsRepository` in Infrastructure. Business logic never touches file I/O directly.

**No-Op stubs**: `NoOpLlmProvider`, `NoOpAudioTranscriber`, and `NoOpMicrophoneRecorder` in Infrastructure allow the app to run without an API key or audio hardware configured. `NoOpMicrophoneRecorder` (Infrastructure/Audio/) returns `false` for `IsMicrophoneAvailable` and throws `InvalidOperationException` on `StartAsync`.

**In-app dictation (microphone recording)**: `IMicrophoneRecorder` interface (Application/Interfaces/) with `IsMicrophoneAvailable`, `StartAsync(string outputFilePath, CancellationToken)`, `StopAsync()`. `WindowsMicrophoneRecorder` (Infrastructure/Audio/) is the concrete implementation using NAudio 2.2.1 `WasapiCapture` + `WaveFileWriter` to capture WASAPI PCM into a temporary `.tmp.wav` file (`Path.ChangeExtension(outputFilePath, ".tmp.wav")`). `StopAsync()` is truly async: wires a `TaskCompletionSource<bool>` to `WasapiCapture.RecordingStopped`, awaits it, flushes/disposes the writer, then on a background thread encodes the temp WAV to MP3 at `outputFilePath` via `MediaFoundationEncoder.EncodeToMp3` (NAudio MediaFoundation) and deletes the temp WAV. The caller always receives an MP3, never a raw WAV. `Dispose()` cleans up capture/writer and deletes the temp WAV if present. `IsMicrophoneAvailable` gracefully returns `false` on any exception (no hardware). `StartAsync` throws `InvalidOperationException("Recording is already in progress.")` on double-start. `NoOpMicrophoneRecorder` (Infrastructure/Audio/) is the offline stub injected in tests. `MainViewModel` exposes `IsRecording` (`[ObservableProperty]`), `RecordingDuration` (live `mm:ss` string updated via `DispatcherTimer`), `StartDictationCommand` (CanExecute = `!IsRecording`; checks `processor.CanProcess` and `microphoneRecorder.IsMicrophoneAvailable`; sets `tempRecordingPath` to an `.mp3` path in `Path.GetTempPath()`), and `StopDictationCommand` (CanExecute = `IsRecording` property directly; stops timer + recorder — recorder internally converts WAV→MP3 — then pipes the MP3 through `processor.ProcessAudioAsync`). Flow: microphone → temp WAV (internal) → MP3 at temp path → `ProcessAudioAsync` → `RefreshAfterEntryAsync`. Tested in `MicrophoneRecordingViewModelTests.cs`. UI: bottom bar of the entry list pane is dual-state — idle shows a full-width "🎙 Diktieren" button (visibility via `InverseBoolToVis`; "+ Neues Element" and `NewEntryView` were removed in v1.4.0); recording shows a pulsing red ellipse (WPF Storyboard, Opacity 1→0.15, 0.8 s, AutoReverse, Forever), "REC" label in `AccentBrush`, `RecordingDuration` timer in `MonoFamily`, and "■ Stop" button docked right (visibility via `BoolToVis`).

**Schema versioning**: `Entry.SchemaVersion` (currently **4**) + `JsonMigrator` handle forward migration of persisted JSON files. v2→v3 added `EditedTranscript`; v3→v4 added `CustomSections` and `CustomSectionNames`. `EntryDto` carries `[JsonExtensionData]` so unknown fields survive a round-trip. **`EntryDto`/`EntryMapper`, `SettingsDto` and `PromptDto` are hand-written mappers — every new field must be added to the DTO *and* both mapping directions. This has silently eaten a field three times (`CustomCategories`, `SectionModes`, `CustomSections`); always add a round-trip test.**

**Settings split**: `AppSettings` holds user preferences (name, company, directories); `PromptSettings` holds all LLM prompt templates. Persisted separately as `settings.json` and `prompts.json`. `SettingsHolder` wraps both for live propagation to `SummaryGenerator`. Internally uses a `volatile` immutable `SettingsState` record so `Snapshot()` always reads a consistent pair. `Update(AppSettings, PromptSettings)` atomically swaps both values; individual `Current`/`Prompts` setters preserved for backward compatibility.

**Prompt text: the team file is the single source of truth.** The team's `prompts.json` (`AppSettings.GlobalPromptFilePath`, typically `Z:\12_Tools\Peano\Johann\prompts.json`) owns the wording of all nine prompts. It always wins at runtime — `JsonPromptSettingsRepository` maps every field as `dto.X ?? defaults.X`, and `ToDto` writes all nine back on every save, so once a file exists its text is authoritative forever. The `SummaryPrompts` constants are **only** the seed for fresh installs and the fallback when the share is unreachable.

Changing prompt wording therefore means changing **both**: edit the team file *and* update the matching constant. `TeamPromptDriftTests` guards this — it compares all nine constants against the team file and silently passes when the share is unreachable (CI, no VPN), so it never turns red for the wrong reason. Set `JOHANN_TEAM_PROMPTS` to point it elsewhere.

⚠ **Never make a client rewrite the team file automatically.** `PromptDefaultsMigration` was exactly that idea — a revision integer that bulk-replaced prompts — and it was deleted in v1.4.0: it was never wired up, would never have fired (`PromptDefaultsRevision` defaults to the current revision, so the guard always short-circuits), and had it worked it would have overwritten curated team wording from whichever machine happened to load the file first. That is the same failure mode as a v1.3.2 client stripping `customCategories`. `PromptSettings.PromptDefaultsRevision` survives only so the JSON key round-trips instead of being stripped on the next save.

**Settings migration**: `SettingsSplitMigration.MigrateIfNeeded` performs a one-time extraction of prompt keys from legacy `settings.json` into `prompts.json`. `SettingsSplitMigration.CleanupLegacyFiles` runs at startup to remove leftover local `prompts.json` and strip any remaining prompt keys from `settings.json` (best-effort, silent on failure).

**Startup path resolution**: `StartupPathResolver` (UI/StartupPathResolver.cs) validates configured directories (Quellverzeichnis, Ausgabeverzeichnis, Archivverzeichnis) at startup, falling back to safe defaults when a path is missing, empty, or uncreateable. Returns `StartupPathResolution` (sealed record) with both `PersistedSettings` (unchanged) and `EffectiveSettings` (with fallback paths applied) plus `IReadOnlyList<StartupPathIssue> Issues` for user-visible warning messages. `App.xaml.cs` creates two `SettingsHolder` instances — `persistedSettingsHolder` (raw stored paths) and `runtimeSettingsHolder` (effective/fallback paths) — so that persisted user settings are never silently overwritten by runtime fallbacks. If issues exist, a warning MessageBox lists each affected path with its configured value, fallback, and reason.

**Korrekturliste (correction list)**: `AppSettings.Korrekturliste` (`IReadOnlyList<CorrectionEntry>`) stores user-defined Whisper transcription corrections (wrong→correct pairs). Persisted in `settings.json` via `JsonSettingsRepository`. `SummaryGenerator.BuildSystemPrompt()` appends them to the LLM system message so GPT silently corrects known transcription errors before summarising. UI: `CorrectionEntryViewModel` wraps each entry for WPF binding; `SettingsViewModel.Korrekturen` (`ObservableCollection`) with `AddCorrection` / `RemoveCorrection` commands; "Korrekturliste" section in `SettingsView` under GRUNDDATEN.

**Editable transcripts**: `Entry.EditedTranscript` stores user corrections to Whisper output; `EffectiveTranscript` (computed) returns edited text if present, otherwise original. `IEntryProcessor.RegenerateFromTranscriptAsync` stores the edited transcript and re-runs all summary generation using the corrected text. `ReprocessAsync` also uses `EffectiveTranscript`. Renderers (`HtmlRenderer`, `PdfRenderer`) and archive use `EffectiveTranscript`. UI: `EntryDetailViewModel` exposes `EditTranscript` / `CancelEditTranscript` / `RegenerateFromTranscript` commands with `IsEditingTranscript` / `EditableTranscriptText` state; `MainWindow.xaml` shows inline edit controls in the transcript section.

**Team-shared prompts**: `AppSettings.GlobalPromptFilePath` points to a shared `prompts.json`. `PromptStartupResolver.ResolveAsync` (Application/Services/) is the single startup entry point: it calls `PromptSettingsLoader.LoadWithFallbackAsync`, mirrors every successful global load into a local cache (`Documents\Johann\prompts.cache.json`), and returns a ready-to-display `Warning` when the share was unreachable or corrupt. The cache means an offline VPN degrades to the last known *team* prompts rather than silently reverting to built-in defaults. `JsonPromptSettingsRepository.FromFilePath` creates a repo for arbitrary file paths; its `LastLoadFault` distinguishes "loaded defaults because the file is fine and empty" from "loaded defaults because the file is corrupt" — the loader treats a fault as a fallback trigger.

**Corrupt settings files**: `JsonSettingsRepository` / `JsonPromptSettingsRepository` no longer swallow parse errors. On failure they copy the unreadable file aside via `CorruptSettingsBackup.Preserve` (`<name>.corrupt-<timestamp>.json` — a copy, never a move, because the shared file may be open elsewhere), expose a `SettingsFileFault` on `LastLoadFault`, and return defaults. `App.OnStartup` collects these into one warning dialog. Without this, the next `SaveAsync` turned a recoverable parse error into permanent data loss.

**CrashLogWriter**: Unified log sink for both unhandled crashes and non-fatal processing warnings (`EntryProcessingService` swallow sites route through `CrashLogEntryProcessingLogger`). Writes to `C:\Peano\Platee.Johann\logs\johann-crash-*.log`; if that location can't be created/written to (e.g. permissions), transparently falls back to `%LOCALAPPDATA%\Peano\Platee.Johann\logs\` so entries are never silently dropped. `CrashLogWriter.LogDirectory` reflects whichever location is currently active.

**WPF MVVM**: `CommunityToolkit.Mvvm 8.4` — ViewModels use `[ObservableProperty]` / `[RelayCommand]` source generators. Single-instance enforcement on `SettingsViewModel`.

**Toast notification tray**: `ToastQueue` (pure, injectable timer factory) + `ToastsViewModel` (WPF `DispatcherTimer` wrapper) replace the former single-toast overlay in `MainViewModel`. `MainViewModel.Toasts` exposes `ObservableCollection<ToastItem>` bound to an `ItemsControl` in `MainWindow.xaml`. Tones: `Ok` (green) / `Warn` (orange) / `Error` (red) derived by `ToastToneHelper`. Auto-dismiss after 5.2 s; hover pauses the timer. Error toasts expose a "Details im Status-Log" link wired to `OpenProcessDetailCommand`.

**Shared formatting helpers**: `DurationFormatter.Format(double seconds)` in `Domain/Services/` (centralised from former UI/Helpers duplicate) is used by `EntryDetailViewModel` (display duration), `EntryRowViewModel.FormattedDuration` (entry-list subtitle), and `PdfRenderer` (header meta). Format: `m:ss` for < 1 h, `h:mm:ss` for ≥ 1 h.

**Finding04State**: Static helper in `UI/ViewModels/Finding04State.cs` centralises the logic for `CanUseDetailActions` / `DetailActionsDisabledReason`. Both `EntryDetailViewModel` and `MainViewModel` delegate to it; tested in `Finding04StateTests.cs`.

**Detail zoom**: `EntryDetailViewModel.DetailZoom` (double, 1.0 default, range 0.5–2.0, step 0.1) drives a `ScaleTransform` on the detail `StackPanel` in `MainWindow.xaml`. `ZoomIn` / `ZoomOut` / `ZoomReset` relay commands exposed; `ZoomText` shows the current percentage. Zoom controls sit in the status bar. Keyboard shortcuts handled in `MainWindow.xaml.cs`: `Ctrl++` / `Ctrl+-` for zoom in/out, `Ctrl+0` for reset to 100 %, `Ctrl+Scroll` for mouse wheel zoom. Tested in `EntryDetailZoomTests.cs`.

**Admin mode for prompt editing**: `SettingsViewModel` exposes `IsAdminMode`, `IsPromptReadOnly`, `AdminButtonLabel`, `ActivateAdmin(password)`, `DeactivateAdmin()`. Password gate controls access to global shared prompt editing; normal mode makes prompts read-only. `AdminPasswordDialog` (`Views/AdminPasswordDialog.xaml`) is a simple WPF dialog for password entry. Visual indicators: red "ADMIN-MODUS AKTIV" banner in `SettingsView`, `AdminAwareWarning` style changes color in admin mode.

**Settings view section navigation**: `SettingsView.xaml` uses a `CollectionViewSource` with `PropertyGroupDescription` for grouped left-sidebar section navigation. Sections are bound to `SettingsViewModel.Sections`; selected section toggles content panel visibility via `Is<Section>Selected` properties.

**Release notes window**: `ReleaseNotesHelper` in `UI/Helpers/` loads `RELEASE_NOTES.md` (embedded resource) and renders it via `MarkdownHelper.ToHtml()` into a styled HTML document displayed in `ReleaseNotesWindow` (WPF `WebBrowser`). `ShouldShow(lastSeenVersion, currentVersion)` gates display to once per version update.

**Embedded user handbook**: `HANDBUCH.html` is an embedded resource in `UI/Assets/`. `MainViewModel.ExtractHandbook()` extracts it to a temp file (`Platee.Johann.HANDBUCH.html`) for display in the default browser. `README.md` (repo root) is the Markdown version of the same handbook content.

**Drag & Drop PDF export**: Dragging an entry from the entry list triggers `EntryDetailViewModel.RenderPdfForDragAsync()` to generate a PDF, then `MainWindow.xaml.cs` executes `DragDrop.DoDragDrop` with the file path. Users can drag entries directly into Explorer, e-mail clients, or other apps.

**JobId format & date-prefix optimization**: JobIds follow the format `YYMMDD_NNN_XXXXXXXX` (e.g. `260617_001_a1b2c3d4`). `GetByJobIdAsync` parses the 6-digit date prefix to target a single date directory instead of scanning all directories (O(day's entries) vs O(total entries)). Falls back to full scan for non-standard JobIds. Private helpers: `TryParseDateFromJobId`, `ScanDirectoryForJobIdAsync`.

**JobId migration**: `IEntryRepository.MigrateJobIdsAsync` (called once at startup in `App.xaml.cs`) rewrites legacy non-standard JobIds to the `YYMMDD_NNN_XXXXXXXX` format so all entries benefit from the date-prefix fast path. Crash-safe: skips individual files that fail to load/save, only rethrows `OperationCanceledException`. Returns a `JobIdMigrationResult(Migrated, Skipped)`; `App` writes any skipped files to the crash log, because a skipped entry stays on the slow lookup path forever. Tested in `EntryRepositoryTests`.

**Settings snapshot for processing**: `SettingsHolder.Snapshot()` atomically captures current `AppSettings` and `PromptSettings` from the single `volatile SettingsState` reference into a new isolated instance, preventing torn reads. `SummaryGenerator.WithSnapshot()` creates a scoped generator using the frozen settings. Every `EntryProcessingService` processing method (`ProcessAudioAsync`, `ReprocessAsync`, `ReprocessSectionAsync`, `RegenerateFromTranscriptAsync`, `GenerateEmailTextAsync`) calls `WithSnapshot()` at the start so all parallel GPT calls within one run use the same prompts, even if the user saves settings mid-flight via the non-modal `SettingsView`. All write sites (`SettingsViewModel.SaveAsync`, `App.OnStartup`) use `SettingsHolder.Update()` for atomic dual-property writes. Tested in `SettingsSnapshotTests` (includes concurrent writer/reader stress test).

<!-- END AUTO-MANAGED -->

<!-- AUTO-MANAGED: git-insights -->
**User-definable categories (v1.3.3)**: `CategoryDefinition` (Application/Settings/) is a
`sealed record` with `Id`/`Name`/`Prompt`/`Scope`/`Order`/`MaxTokens`. `BuiltInSections` holds the
seven stable ids for the built-in sections (`builtin.longSummary` …); Title and Abstract are
intrinsics and deliberately absent. `CategoryIdFactory.Create(name, existingIds, suffixFactory?)`
mints `custom.<slug>-<4 hex>` — **the random suffix is load-bearing**: without it, deleting a
category and creating another frees the id and the old category's orphaned text re-attaches to the
new one. Ids are minted on **first save** (from the name the user actually typed) and never change
afterwards, because generated text is keyed by id.

**Section catalog & generation modes**: `SectionCatalog.Build(prompts, modes)` produces
`SectionDescriptor(Id, Name, Mode, IsBuiltIn, Category)`. `GenerationMode` is `Auto` or `OnDemand`,
stored per user in `AppSettings.SectionModes`. `SectionModeDefaults.Recommended` runs four built-ins
automatically instead of eight; `SectionModeMigration` drives a one-time first-run dialog
(`SectionModeMigrationDialog`). `EntryProcessingService.GenerateSectionAsync(entry, sectionId, …)`
generates exactly one section and coalesces concurrent calls for the same (entry, section) through a
`ConcurrentDictionary` — a UI CanExecute flag is not enough, since each duplicate click costs money.

**Category storage & scope**: the team `prompts.json` (`AppSettings.GlobalPromptFilePath`, typically
on `Z:`) owns the eight built-in prompts and global categories; `prompts.personal.json` in
`Documents\Johann` owns **only** the user's own categories — never prompt text, so a user can never
be frozen out of team prompt updates. `Scope` is derived from which file a category came from and is
never persisted. `PromptSettingsLoader.MergeCategories` merges them, personal winning on id.
`PromptStartupResolver.ResolveAsync` runs at startup; `SettingsViewModel.ReloadTeamPromptsAsync`
re-runs the merge when the configured team path changes. **v1.3.2 clients strip `customCategories`
from the team file on save** — documented, unfixable, hence the warning not to create global
categories until the whole team has upgraded.

**Section visibility**: `SectionVisibilityViewModel` has a fixed property per built-in section plus
`CustomSectionVisibility` (id → bool, missing key = visible) and `CustomSections`, one
`CustomSectionToggleViewModel` per category, split into `PersonalSections` / `GlobalSections` /
`OrphanedSections` for the grouped sidebar. `SyncCustomSections(catalog, entrySections,
entrySectionNames)` is rebuilt on entry selection and takes the **entry** as well as the catalog, so
text whose category was deleted still gets a toggle — otherwise it could not be hidden anywhere.
All four outputs (detail view, PDF, HTML, clipboard) honour this map.

**Deleted-category tombstones**: `Entry.CustomSectionNames` records the display name each custom
section was generated under. Resolution order everywhere is *current catalog → recorded name → raw
id*, so renames reach the exports while deleted categories stay readable.

**Save target replaces the admin password**: `SettingsViewModel.SaveTarget` (`CategoryScope`,
default `Personal`) decides which file prompts go to. The `AdminPasswordDialog` is deleted. If the
global target is unwritable, the save falls back to personal and the status message states exactly
which half was rescued — prompt text is team-owned and survives only for the session.

## Git Insights

- **v1.4.0** (2026-09-10, released): the first release since v1.3.2. Renumbered from the
  unreleased v1.3.3 under the new rule — minor for anything users see, patch for developer
  intermediates — so the category rework shipped inside it rather than getting its own release.
  Contains #63 (days vanished from the sidebar when a day was fully done), #62 ("+ Neues Element"
  and `NewEntryView` deleted), #66 (Aufgaben prompt: summary then checkable tasks), #59
  ("Kategorien" renamed to "Vorlagen" in the UI), #67 (`gpt-transcribe` + `gpt-5.6-luna`,
  duration measured locally) and #58 (foreign-language dictations yield German entries).
  Three display bugs surfaced only because the stronger model produced real structure:
  flattened nested lists, raw Markdown in the prose summary and abstract, and prompts that
  repeated the section heading the app already renders.

- **Category rework, v1.3.3** (`v1.3.3-dev` tag, merged to `main` 2026-09-09, **not released**):
  #50–#53 — custom categories, auto vs. on-demand, password gate removed, section visibility,
  tombstones. A full manual test pass found twelve defects, all fixed with tests; the notable ones
  were persisted detail-view edits lost on re-selection (`EntryRowViewModel` held a stale immutable
  `Entry`; fixed with `EntryDetailViewModel.EntryUpdated`), category ids reused after deletion, and
  custom sections missing from the clipboard and the daily overview. Users remain on v1.3.2.
- **Clean Architecture introduced** gradually — Infrastructure and Application were split to isolate LLM dependencies.
- **Settings path fallback** (`1d0716f`): startup now shows meaningful feedback when `.env` is missing rather than silently failing.
- **HTML hardening** (`acfd293`): `HtmlRenderer` sanitises user content to prevent XSS in the embedded WebView.
- **Prompt migration** (`fb22129`): one-time migration system ensures default prompts update for existing installs without overwriting user customisations.
- **CrashLogWriter** (`835a9f5`): structured crash logs with version, timestamp, and full stack trace.
- **CrashLogWriter unified path + fallback rework**: default log location moved from `%UserProfile%\Peano\Johann\logs` to a fixed `C:\Peano\Platee.Johann\logs`, shared across all users on a machine. Constructor changed to `CrashLogWriter(string? primaryRootPath = null, string? appVersion = null, ICrashLogFileSystem? fileSystem = null, Func<DateTimeOffset>? utcNow = null, string? fallbackRootPath = null)` — both roots are now optional, defaulting to `C:\` and `%LOCALAPPDATA%` respectively. `LogDirectory` is a computed property reflecting whichever of `primaryDirectory` / `fallbackDirectory` is currently active, so it can flip between calls if the primary becomes writable/unwritable. `App.xaml.cs` now constructs it with just `appVersion:` (no explicit path). Also fixed a pre-existing swallowed-exception gap: warnings from `CrashLogEntryProcessingLogger` now share the same fallback path, so they're never silently dropped when the primary directory is inaccessible.
- **Sprint 3 UX findings 11 & 12** (`409beec` / `39c9b28`): entry-list subtitle enriched with `TypeBadge · duration` via shared `DurationFormatter`; single-toast overlay in `MainViewModel` replaced with a queue-based multi-toast tray (`ToastQueue` / `ToastsViewModel` / `ToastView`).
- **SonarCloud CI** (`a9c1316` / `da8ecc7` / `323ec03`): `.github/workflows/build.yml` runs SonarCloud analysis on push-to-main and PRs (windows-latest, JDK 17 zulu). Config is inline via scanner flags: project key `jonasyr_Platee.Johann`, org `gitray-org`, OpenCover coverage at `**/TestResults/**/coverage.opencover.xml`. `sonar-project.properties` was removed (`323ec03`) — all config now lives in the workflow.
- **Settings split**: Prompt configuration extracted from monolithic `AppSettings` into dedicated `PromptSettings` record with separate persistence (`prompts.json`). `SettingsSplitMigration` handles one-time data migration. `PromptSettingsLoader` adds local/global fallback to enable team-shared prompts via `GlobalPromptFilePath`.
- **Admin mode** (`e6e1486` / `0d32274` / `56e34e6` / `e0fba8c`): password-gated admin mode for editing global shared prompts in `SettingsView`. `AdminPasswordDialog` added for password entry. `SettingsSplitMigration.CleanupLegacyFiles` runs at startup to remove leftover local prompt files. `AdminAwareWarning` XAML style extracted to reduce duplication.
- **Release notes window** (`3968a67`): `ReleaseNotesWindow` with embedded `RELEASE_NOTES.md` rendered via `MarkdownHelper`; version-gated display via `ReleaseNotesHelper.ShouldShow()`.
- **Embedded handbook** (`639a0e2`): `HANDBUCH.html` added as embedded resource; `MainViewModel.ExtractHandbook()` extracts to temp file for browser display.
- **Auto-copy docs to Assets** (`98dd750`): MSBuild `CopyDocsToAssets` target copies `HANDBUCH.html` and `RELEASE_NOTES.md` from repo root into `Assets/` before build, keeping embedded resources in sync with source docs.
- **Editable transcripts / Schema v3** (`0dadb19` .. `c0c5aaf`): `EditedTranscript` field added to `Entry` (schema v3). Inline transcript editing in detail view with regenerate-from-corrected-text flow. `EffectiveTranscript` computed property used across renderers, archive, and reprocessing. Three new test classes: `EditableTranscriptTests`, `EntryDetailTranscriptEditTests`, `RegenerateFromTranscriptTests`.
- **Velopack 1.2.0** (`9d54c72`): upgraded installer SDK from pre-release 0.0.1298 to stable 1.2.0.
- **Zoom keyboard shortcuts** (`08a582f`): `Ctrl++` / `Ctrl+-` / `Ctrl+0` / `Ctrl+Scroll` shortcuts added for detail view zoom. `ZoomResetCommand` added to `EntryDetailViewModel`. `EntryDetailZoomTests` added.
- **v1.3.0 documentation** (`7a93e0e` / `4d1f57d` / `1ddfbfd` / `08a582f`): `README.md`, `HANDBUCH.html`, and `RELEASE_NOTES.md` updated with transcript editing, Korrekturliste, drag & drop, and zoom keyboard shortcut features.
- **GetByJobIdAsync optimization** (`74dc2bd`): date-prefix parsing reduces lookup from O(total entries) to O(single day). `TryParseDateFromJobId` + `ScanDirectoryForJobIdAsync` helpers extracted; fallback to full scan for non-standard JobIds.
- **JobId migration** (`0cb97e6` / `c1a2919`): one-time startup migration rewrites legacy JobIds to standard `YYMMDD_NNN_XXXXXXXX` format. Made crash-safe with per-file error handling to prevent a single corrupt file from aborting the entire migration.
- **Settings snapshot** (`75ef5b8` / `d350fae`): `SettingsHolder` rewritten with `volatile` inner `SettingsState` record for tear-free atomic snapshots. `Update(AppSettings, PromptSettings)` added for atomic dual writes; all write sites in `SettingsViewModel` and `App.xaml.cs` migrated. `SummaryGenerator.WithSnapshot()` freezes settings at processing start. Concurrent stress test in `SettingsSnapshotTests` validates no torn reads.
- **IMicrophoneRecorder + NoOp stub** (`fd1c2f2`): `IMicrophoneRecorder` interface added to Application/Interfaces/ (`IsMicrophoneAvailable`, `StartAsync`, `StopAsync`). `NoOpMicrophoneRecorder` sealed stub added to Infrastructure/Audio/ — returns `false` for availability, throws `InvalidOperationException` on start. NAudio 2.2.1 added to Infrastructure project.
- **WindowsMicrophoneRecorder** (`252680b`): Concrete NAudio implementation of `IMicrophoneRecorder` in Infrastructure/Audio/. Uses `WasapiCapture` + `WaveFileWriter` for WASAPI PCM capture to a temp WAV, then encodes to MP3 via `MediaFoundationEncoder.EncodeToMp3` in `StopAsync()`. `IsMicrophoneAvailable` uses `MMDeviceEnumerator` with graceful fallback. Implements `IDisposable` for safe cleanup.
- **MainViewModel dictation integration** (`7f4b35e`): `StartDictationCommand` / `StopDictationCommand` wired into `MainViewModel` with `IsRecording` + `RecordingDuration` observable state. Guard checks for API key and microphone availability before starting. `IMicrophoneRecorder` injected via constructor; `WindowsMicrophoneRecorder` used when API key present, `NoOpMicrophoneRecorder` otherwise. Test project extended with Compile + Page links for `MainViewModel.cs`, `ToastsViewModel.cs`, `SortMode.cs`, and WPF views (`SettingsView`, `NewEntryView`, `AdminPasswordDialog`). Covered by `MicrophoneRecordingViewModelTests` (8 xUnit tests using `NoOpMicrophoneRecorder`).
- **IMicrophoneRecorder DI wiring fix** (`12d0bbb`): `App.xaml.cs` `OnStartup` replaced simple `apiKey`-based ternary for `IMicrophoneRecorder` with try/catch + `IsMicrophoneAvailable` check — instantiates `WindowsMicrophoneRecorder`, uses it only if `IsMicrophoneAvailable` is true, falls back to `NoOpMicrophoneRecorder` on exception or unavailability regardless of API key presence.
- **Diktieren recording UI** (`816a277`): entry list pane bottom bar extended to dual-state XAML — idle showed "+ Neues Element" + "🎙 Diktieren" buttons (the former removed in v1.4.0); recording state shows pulsing red ellipse (WPF Storyboard), "REC" label, live timer, and "■ Stop" button. Binds to existing `IsRecording`, `RecordingDuration`, `StartDictationCommand`, `StopDictationCommand` properties via `BoolToVis` / `InverseBoolToVis` converters.
- **WindowsMicrophoneRecorder dispose fix** (`65db67a`): `App.xaml.cs` now calls `realRecorder.Dispose()` before assigning `NoOpMicrophoneRecorder` when `IsMicrophoneAvailable` returns false, preventing a resource leak when microphone hardware is present but unavailable.
- **WindowsMicrophoneRecorder robustness** (`1784c09`): `StartAsync` now throws `InvalidOperationException` on double-start. `StopAsync` made truly async — uses `TaskCompletionSource` wired to `RecordingStopped` event and awaits it before flushing/disposing the WAV writer, guaranteeing the file is complete before the caller proceeds.
- **v1.3.0 documentation** (`ce30ade`): `RELEASE_NOTES.md` updated with in-app dictation feature notes for end users.
- **In-app dictation documentation** (`5be0a71`): `README.md` and `HANDBUCH.html` updated with "Direkt diktieren (Mikrofon)" workflow section describing the 🎙 Diktieren button flow and requirements (microphone + API key).
- **WAV→MP3 conversion in recorder**: `WindowsMicrophoneRecorder.StopAsync()` encodes the captured temp WAV to MP3 via `MediaFoundationEncoder.EncodeToMp3` (NAudio MediaFoundation) before returning, so `ProcessAudioAsync` always receives an MP3 regardless of capture format. `MainViewModel.StartDictation` sets `tempRecordingPath` to a `.mp3` path directly.

<!-- END AUTO-MANAGED -->

<!-- MANUAL -->
## Custom Notes

- Install repo hooks with `./scripts/install-hooks.ps1`.
- Pre-commit runs quick hygiene checks and auto-formats staged C# files via `dotnet-format` (run `dotnet tool restore` once).
- Pre-push runs `dotnet build` and `dotnet test` with `--no-restore`.

<!-- Add project-specific notes here. This section is never auto-modified. -->

<!-- END MANUAL -->
