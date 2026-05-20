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
  Interfaces/                  # IEntryRepository, ILlmProvider, IAudioTranscriber, …
  Processing/                  # EntryProcessingService, SummaryGenerator, AudioWatcherService
  Settings/                    # AppSettings (immutable record), SettingsHolder, migration

Platee.Johann.Infrastructure/  # Concrete adapters (depends on Application + Domain)
  Json/                        # JsonRepository (file-backed), JsonSettingsRepository, migration
  Llm/                         # OpenAiLlmProvider, WhisperTranscriber, NoOp stubs
  Renderers/                   # HtmlRenderer, PdfRenderer, EmailRenderer, HtmlOverviewService

Platee.Johann.UI/              # WPF presentation layer (depends on all)
  ViewModels/                  # MainViewModel, SettingsViewModel, NewEntryViewModel, …
  Views/                       # NewEntryView.xaml, SettingsView.xaml
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

**Repository pattern**: `IEntryRepository` / `ISettingsRepository` interfaces in Application; `JsonRepository` / `JsonSettingsRepository` in Infrastructure. Business logic never touches file I/O directly.

**No-Op stubs**: `NoOpLlmProvider` and `NoOpAudioTranscriber` in Infrastructure allow the app to run without an API key configured.

**Schema versioning**: `Entry.SchemaVersion` (currently 2) + `JsonMigrator` handle forward migration of persisted JSON files.

**Settings migration**: `PromptDefaultsMigration` uses a revision integer to apply one-time migrations to user settings without overwriting manual customisations.

**CrashLogWriter**: Unhandled-exception handler writes to `%LOCALAPPDATA%\Platee\Johann\crash-*.log`.

**WPF MVVM**: `CommunityToolkit.Mvvm 8.4` — ViewModels use `[ObservableProperty]` / `[RelayCommand]` source generators. Single-instance enforcement on `SettingsViewModel`.

<!-- END AUTO-MANAGED -->

<!-- AUTO-MANAGED: git-insights -->
## Git Insights

- **Clean Architecture introduced** gradually — Infrastructure and Application were split to isolate LLM dependencies.
- **Settings path fallback** (`1d0716f`): startup now shows meaningful feedback when `.env` is missing rather than silently failing.
- **HTML hardening** (`acfd293`): `HtmlRenderer` sanitises user content to prevent XSS in the embedded WebView.
- **Prompt migration** (`fb22129`): one-time migration system ensures default prompts update for existing installs without overwriting user customisations.
- **CrashLogWriter** (`835a9f5`): structured crash logs with version, timestamp, and full stack trace.

<!-- END AUTO-MANAGED -->

<!-- MANUAL -->
## Custom Notes

<!-- Add project-specific notes here. This section is never auto-modified. -->

<!-- END MANUAL -->
