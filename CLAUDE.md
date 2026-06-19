# Platé.Johann

<!-- AUTO-MANAGED: project-description -->
## Overview

**Platé.Johann** is an AI-powered dictation-to-journal tool for Windows. Users drop an MP3 recorded on their smartphone into a watch folder; Johann automatically transcribes it via OpenAI Whisper, generates structured summaries with GPT, and archives the result as HTML and PDF.

Key features:
- Automatic MP3 watch-folder processing (FileSystemWatcher)
- OpenAI Whisper transcription + GPT summarisation
- Inline transcript editing with regeneration from corrected text
- Five entry types: Aufgabe, E-Mail, Gesprächsnotiz, Stundenzettel, Analog
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
  Enums/EntryType.cs
  Parsing/                     # Header, title, type extraction from filenames
  Services/                    # DurationFormatter (shared formatting helper)
  ValueObjects/                # ParsedHeader, ProcessingStatus, CorrectionEntry

Platee.Johann.Application/     # Use-cases, interfaces (depends on Domain only)
  Interfaces/                  # IEntryRepository, ILlmProvider, IAudioTranscriber,
                               #   IPromptSettingsRepository
  Processing/                  # EntryProcessingService, SummaryGenerator, AudioWatcherService
  Services/                    # PromptSettingsLoader (local/global fallback)
  Settings/                    # AppSettings, PromptSettings, SettingsHolder,
                               #   PromptDefaultsMigration, SettingsSplitMigration

Platee.Johann.Infrastructure/  # Concrete adapters (depends on Application + Domain)
  Json/                        # JsonRepository (file-backed), JsonSettingsRepository,
                               #   JsonPromptSettingsRepository, migration
  Llm/                         # OpenAiLlmProvider, WhisperTranscriber, NoOp stubs
  Renderers/                   # HtmlRenderer, PdfRenderer, EmailRenderer, HtmlOverviewService

Platee.Johann.UI/              # WPF presentation layer (depends on all)
  Assets/                      # RELEASE_NOTES.md, HANDBUCH.html (embedded resources,
                               #   auto-copied from repo root via CopyDocsToAssets MSBuild target)
  Helpers/                     # DurationFormatter, ReleaseNotesHelper — pure static helpers
  ViewModels/                  # MainViewModel, SettingsViewModel, NewEntryViewModel,
                               #   CorrectionEntryViewModel, …
                               #   Toast stack: ToastTone, ToastToneHelper, ToastItem,
                               #                ToastQueue, ToastsViewModel
  Views/                       # AdminPasswordDialog.xaml, NewEntryView.xaml,
                               #   ReleaseNotesWindow.xaml, SettingsView.xaml,
                               #   ToastView.xaml
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

**Repository pattern**: `IEntryRepository` / `ISettingsRepository` / `IPromptSettingsRepository` interfaces in Application; `JsonRepository` / `JsonSettingsRepository` / `JsonPromptSettingsRepository` in Infrastructure. Business logic never touches file I/O directly.

**No-Op stubs**: `NoOpLlmProvider` and `NoOpAudioTranscriber` in Infrastructure allow the app to run without an API key configured.

**Schema versioning**: `Entry.SchemaVersion` (currently 3) + `JsonMigrator` handle forward migration of persisted JSON files. v2→v3 added `EditedTranscript` field.

**Settings split**: `AppSettings` holds user preferences (name, company, directories); `PromptSettings` holds all LLM prompt templates. Persisted separately as `settings.json` and `prompts.json`. `SettingsHolder` wraps both for live propagation to `SummaryGenerator`.

**Settings migration**: `PromptDefaultsMigration` uses a revision integer to apply one-time prompt migrations without overwriting user customisations. `SettingsSplitMigration.MigrateIfNeeded` performs a one-time extraction of prompt keys from legacy `settings.json` into `prompts.json`. `SettingsSplitMigration.CleanupLegacyFiles` runs at startup to remove leftover local `prompts.json` and strip any remaining prompt keys from `settings.json` (best-effort, silent on failure).

**Korrekturliste (correction list)**: `AppSettings.Korrekturliste` (`IReadOnlyList<CorrectionEntry>`) stores user-defined Whisper transcription corrections (wrong→correct pairs). Persisted in `settings.json` via `JsonSettingsRepository`. `SummaryGenerator.BuildSystemPrompt()` appends them to the LLM system message so GPT silently corrects known transcription errors before summarising. UI: `CorrectionEntryViewModel` wraps each entry for WPF binding; `SettingsViewModel.Korrekturen` (`ObservableCollection`) with `AddCorrection` / `RemoveCorrection` commands; "Korrekturliste" section in `SettingsView` under GRUNDDATEN.

**Editable transcripts**: `Entry.EditedTranscript` stores user corrections to Whisper output; `EffectiveTranscript` (computed) returns edited text if present, otherwise original. `IEntryProcessor.RegenerateFromTranscriptAsync` stores the edited transcript and re-runs all summary generation using the corrected text. `ReprocessAsync` also uses `EffectiveTranscript`. Renderers (`HtmlRenderer`, `PdfRenderer`) and archive use `EffectiveTranscript`. UI: `EntryDetailViewModel` exposes `EditTranscript` / `CancelEditTranscript` / `RegenerateFromTranscript` commands with `IsEditingTranscript` / `EditableTranscriptText` state; `MainWindow.xaml` shows inline edit controls in the transcript section.

**Team-shared prompts**: `AppSettings.GlobalPromptFilePath` points to a shared `prompts.json`. `PromptSettingsLoader.LoadWithFallbackAsync` tries global first, falls back to local on failure. `JsonPromptSettingsRepository.FromFilePath` factory creates a repo for arbitrary file paths.

**CrashLogWriter**: Unhandled-exception handler writes to `%LOCALAPPDATA%\Platee\Johann\crash-*.log`.

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

<!-- END AUTO-MANAGED -->

<!-- AUTO-MANAGED: git-insights -->
## Git Insights

- **Clean Architecture introduced** gradually — Infrastructure and Application were split to isolate LLM dependencies.
- **Settings path fallback** (`1d0716f`): startup now shows meaningful feedback when `.env` is missing rather than silently failing.
- **HTML hardening** (`acfd293`): `HtmlRenderer` sanitises user content to prevent XSS in the embedded WebView.
- **Prompt migration** (`fb22129`): one-time migration system ensures default prompts update for existing installs without overwriting user customisations.
- **CrashLogWriter** (`835a9f5`): structured crash logs with version, timestamp, and full stack trace.
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

<!-- END AUTO-MANAGED -->

<!-- MANUAL -->
## Custom Notes

- Install repo hooks with `./scripts/install-hooks.ps1`.
- Pre-commit runs quick hygiene checks and auto-formats staged C# files via `dotnet-format` (run `dotnet tool restore` once).
- Pre-push runs `dotnet build` and `dotnet test` with `--no-restore`.

<!-- Add project-specific notes here. This section is never auto-modified. -->

<!-- END MANUAL -->
