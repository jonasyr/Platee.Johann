# Platé.Johann

<!-- AUTO-MANAGED: project-description -->
## Overview

**Platé.Johann** is an AI-powered dictation-to-journal tool for Windows. Users drop an MP3 recorded on their smartphone into a watch folder; Johann automatically transcribes it via OpenAI Whisper, generates structured summaries with GPT, and archives the result as HTML and PDF.

Key features:
- Automatic MP3 watch-folder processing (FileSystemWatcher)
- OpenAI Whisper transcription + GPT summarisation
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
  Enums/EntryType.cs
  Parsing/                     # Header, title, type extraction from filenames
  ValueObjects/                # ParsedHeader, ProcessingStatus

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
  Helpers/                     # DurationFormatter — pure static formatting helpers
  ViewModels/                  # MainViewModel, SettingsViewModel, NewEntryViewModel, …
                               #   Toast stack: ToastTone, ToastToneHelper, ToastItem,
                               #                ToastQueue, ToastsViewModel
  Views/                       # NewEntryView.xaml, SettingsView.xaml, ToastView.xaml
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

**Schema versioning**: `Entry.SchemaVersion` (currently 2) + `JsonMigrator` handle forward migration of persisted JSON files.

**Settings split**: `AppSettings` holds user preferences (name, company, directories); `PromptSettings` holds all LLM prompt templates. Persisted separately as `settings.json` and `prompts.json`. `SettingsHolder` wraps both for live propagation to `SummaryGenerator`.

**Settings migration**: `PromptDefaultsMigration` uses a revision integer to apply one-time prompt migrations without overwriting user customisations. `SettingsSplitMigration` performs a one-time extraction of prompt keys from legacy `settings.json` into `prompts.json`.

**Team-shared prompts**: `AppSettings.GlobalPromptFilePath` points to a shared `prompts.json`. `PromptSettingsLoader.LoadWithFallbackAsync` tries global first, falls back to local on failure. `JsonPromptSettingsRepository.FromFilePath` factory creates a repo for arbitrary file paths.

**CrashLogWriter**: Unhandled-exception handler writes to `%LOCALAPPDATA%\Platee\Johann\crash-*.log`.

**WPF MVVM**: `CommunityToolkit.Mvvm 8.4` — ViewModels use `[ObservableProperty]` / `[RelayCommand]` source generators. Single-instance enforcement on `SettingsViewModel`.

**Toast notification tray**: `ToastQueue` (pure, injectable timer factory) + `ToastsViewModel` (WPF `DispatcherTimer` wrapper) replace the former single-toast overlay in `MainViewModel`. `MainViewModel.Toasts` exposes `ObservableCollection<ToastItem>` bound to an `ItemsControl` in `MainWindow.xaml`. Tones: `Ok` (green) / `Warn` (orange) / `Error` (red) derived by `ToastToneHelper`. Auto-dismiss after 5.2 s; hover pauses the timer. Error toasts expose a "Details im Status-Log" link wired to `OpenProcessDetailCommand`.

**Shared formatting helpers**: `DurationFormatter.Format(double seconds)` in `UI/Helpers/` is used by both `EntryDetailViewModel` (display duration) and `EntryRowViewModel.FormattedDuration` (entry-list subtitle). Format: `m:ss` for < 1 h, `h:mm:ss` for ≥ 1 h.

**Finding04State**: Static helper in `UI/ViewModels/Finding04State.cs` centralises the logic for `CanUseDetailActions` / `DetailActionsDisabledReason`. Both `EntryDetailViewModel` and `MainViewModel` delegate to it; tested in `Finding04StateTests.cs`.

**Detail zoom**: `EntryDetailViewModel.DetailZoom` (double, 1.0 default, range 0.5–2.0, step 0.1) drives a `ScaleTransform` on the detail `StackPanel` in `MainWindow.xaml`. `ZoomIn` / `ZoomOut` relay commands exposed; `ZoomText` shows the current percentage. Zoom controls sit in the status bar.

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

<!-- END AUTO-MANAGED -->

<!-- MANUAL -->
## Custom Notes

- Install repo hooks with `./scripts/install-hooks.ps1`.
- Pre-commit runs quick hygiene checks and auto-formats staged C# files via `dotnet-format` (run `dotnet tool restore` once).
- Pre-push runs `dotnet build` and `dotnet test` with `--no-restore`.

<!-- Add project-specific notes here. This section is never auto-modified. -->

<!-- END MANUAL -->
