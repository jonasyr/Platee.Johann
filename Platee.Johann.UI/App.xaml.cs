namespace Platee.Johann.UI;

using System.IO;
using System.Text;
using System.Windows;
using Platee.Johann.Application.Diagnostics;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Services;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Parsing;
using Platee.Johann.Infrastructure.Audio;
using Platee.Johann.Infrastructure.Json;
using Platee.Johann.Infrastructure.Llm;
using Platee.Johann.Infrastructure.Renderers;
using Platee.Johann.UI.Helpers;
using Platee.Johann.UI.ViewModels;
using Platee.Johann.UI.Views;
using Velopack;
using Velopack.Sources;

public partial class App : System.Windows.Application
{
    private AudioWatcherService? audioWatcher;

    protected override async void OnStartup(StartupEventArgs e)
    {
        var crashLogger = new CrashLogWriter(
            appVersion: typeof(App).Assembly.GetName().Version?.ToString());
        crashLogger.EnsureLogDirectory();

        this.DispatcherUnhandledException += (_, ex) =>
        {
            crashLogger.WriteCrashLog("DISPATCHER", ex.Exception);
            ex.Handled = false;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            crashLogger.WriteCrashLog("UNHANDLED", ex.ExceptionObject);
        };
        TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
            crashLogger.WriteCrashLog("TASK", ex.Exception);
        };

        base.OnStartup(e);

        // ── Settings ──────────────────────────────────────────────────────────
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann");
        var jsonSettingsRepo = new JsonSettingsRepository(settingsDir);
        ISettingsRepository settingsRepo = jsonSettingsRepo;

        var startupFaults = new List<string>();

        var persistedSettings = await settingsRepo.LoadAsync();
        if (jsonSettingsRepo.LastLoadFault is { } settingsFault)
        {
            startupFaults.Add(DescribeFault("Einstellungen (settings.json)", settingsFault));
        }

        // Clean up legacy local prompt files (one-time, idempotent)
        if (SettingsSplitMigration.CleanupLegacyFiles(settingsDir) is { } cleanupError)
        {
            crashLogger.WriteCrashLog("SETTINGS-CLEANUP", new InvalidOperationException(cleanupError));
        }

        // ── Prompt settings ───────────────────────────────────────────────────
        // The team's global prompt file is the source of truth. Every successful
        // load is mirrored into a local cache so an unreachable share degrades to
        // the last known team prompts instead of silently reverting to built-in
        // defaults — which produced different GPT output than every colleague
        // with nothing on screen to say so (#45 H1).
        // The cache file name carries a digest of the share path, so switching to a
        // different team share cannot serve the previous share's prompts (PR #46).
        var globalPromptPath = persistedSettings.GlobalPromptFilePath;
        var promptCacheRepo = JsonPromptSettingsRepository.FromFilePath(Path.Combine(
            settingsDir,
            PromptStartupResolver.CacheFileNameFor(globalPromptPath ?? string.Empty)));

        JsonPromptSettingsRepository? globalPromptRepo = null;
        if (!string.IsNullOrWhiteSpace(globalPromptPath))
        {
            globalPromptRepo = JsonPromptSettingsRepository.FromFilePath(globalPromptPath);
        }

        var promptStartup = await PromptStartupResolver.ResolveAsync(
            promptCacheRepo,
            globalPromptRepo,
            persistedSettings.GlobalPromptFilePath,
            ex => crashLogger.WriteCrashLog("PROMPT-CACHE", ex));

        var effectivePrompts = promptStartup.Prompts;

        if (promptStartup.Warning is { } promptWarning)
        {
            startupFaults.Add(promptWarning);

            // Only relevant alongside a fallback: when the share loaded fine the
            // cache has just been rewritten, so a corrupt cache healed itself and
            // there is nothing for the user to act on.
            if (promptCacheRepo.LastLoadFault is { } cacheFault)
            {
                startupFaults.Add(DescribeFault("Prompt-Zwischenspeicher", cacheFault));
            }
        }

        var pathResolution = StartupPathResolver.Resolve(
            persistedSettings,
            e.Args,
            ValidateDirectory,
            ResolveDefaultInputRoot,
            ResolveDefaultOutputRoot);

        var effectiveSettings = pathResolution.EffectiveSettings;
        var outputRoot = effectiveSettings.Ausgabeverzeichnis;


        if (pathResolution.Issues.Count > 0)
        {
            MessageBox.Show(
                BuildPathWarningMessage(pathResolution.Issues),
                "Platé.Johann – Verzeichnisse angepasst",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        if (startupFaults.Count > 0)
        {
            MessageBox.Show(
                BuildSettingsFaultMessage(startupFaults),
                "Platé.Johann – Einstellungen konnten nicht geladen werden",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        var persistedSettingsHolder = new SettingsHolder(persistedSettings, effectivePrompts);
        var runtimeSettingsHolder = new SettingsHolder(effectiveSettings, effectivePrompts);

        // ── .env-Prüfung ──────────────────────────────────────────────────────
        EnsureEnvFile(settingsDir);

        // ── Manual DI ─────────────────────────────────────────────────────────
        IEntryRepository repository = new JsonRepository(outputRoot);
        var jobIdMigration = await repository.MigrateJobIdsAsync();
        if (jobIdMigration.Skipped.Count > 0)
        {
            crashLogger.WriteCrashLog(
                "JOBID-MIGRATION",
                new InvalidOperationException(
                    $"{jobIdMigration.Skipped.Count} Eintrag/Einträge konnten nicht migriert werden:" +
                    Environment.NewLine + string.Join(Environment.NewLine, jobIdMigration.Skipped)));
        }

        // HTML overview service — regenerates _ItemÜbersicht.html after every save
        IHtmlOverviewService overviewService = new HtmlOverviewService(repository, outputRoot);

        IEntryRenderer[] renderers =
        [
            new PdfRenderer(runtimeSettingsHolder),
            new HtmlRenderer(overviewService),   // updates overview after HTML export
            new EmailRenderer(),
        ];

        // OpenAI providers — fall back to NoOp if no API key is configured
        var apiKey = ApiKeyProvider.TryGetOpenAiKey();

        ILlmProvider llmProvider = apiKey is not null
            ? new OpenAiLlmProvider(apiKey)
            : new NoOpLlmProvider();

        IAudioTranscriber transcriber = apiKey is not null
            ? new WhisperTranscriber(apiKey)
            : new NoOpAudioTranscriber();

        var summaryGenerator = new SummaryGenerator(llmProvider, runtimeSettingsHolder);
        IEntryProcessor processor = new EntryProcessingService(
            transcriber, summaryGenerator, new HeaderParser(), repository,
            outputRoot, overviewService, runtimeSettingsHolder, renderers,
            new CrashLogEntryProcessingLogger(crashLogger));

        this.audioWatcher = new AudioWatcherService(processor, runtimeSettingsHolder);

        // OnExit does not run when the process dies on an unhandled exception,
        // so register a second cleanup path. Both Dispose implementations are
        // idempotent, so running twice is safe.
        AppDomain.CurrentDomain.ProcessExit += (_, _) => this.DisposeServices();

        IMicrophoneRecorder microphoneRecorder;
        try
        {
            var realRecorder = new WindowsMicrophoneRecorder();
            if (realRecorder.IsMicrophoneAvailable)
            {
                microphoneRecorder = realRecorder;
            }
            else
            {
                realRecorder.Dispose();
                microphoneRecorder = new NoOpMicrophoneRecorder();
            }
        }
        catch
        {
            microphoneRecorder = new NoOpMicrophoneRecorder();
        }

        // ── Window ────────────────────────────────────────────────────────────
        var viewModel = new MainViewModel(repository, renderers, outputRoot, processor,
                                           settingsRepo, persistedSettingsHolder,
                                           runtimeSettingsHolder, microphoneRecorder,
                                           pathResolution.Issues);

        // Track per-file log items for the watcher
        var watcherLogs = new System.Collections.Concurrent.ConcurrentDictionary<string, ProcessLogItem>();

        this.audioWatcher.EntryProcessingProgress += (filePath, progress) =>
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var fileName = Path.GetFileName(filePath);
                var existing = watcherLogs.GetValueOrDefault(filePath);
                if (existing is null)
                {
                    var item = viewModel.AddProcessLog($"{fileName}: {progress.Stage}", isRunning: true);
                    watcherLogs[filePath] = item;
                }
                else
                {
                    existing.Message = $"{fileName}: {progress.Stage}";
                    viewModel.UpdateToastProgress($"{fileName}: {progress.Stage}");
                }
            });

        this.audioWatcher.EntryProcessed += (filePath, entry) =>
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                viewModel.NotifyEntryProcessed(entry);
                if (watcherLogs.TryRemove(filePath, out var logItem))
                {
                    viewModel.CompleteProcessLog(logItem, $"✓ {entry.Title}");
                }
            });

        this.audioWatcher.EntryProcessingFailed += (filePath, ex) =>
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var fileName = Path.GetFileName(filePath);
                if (watcherLogs.TryRemove(filePath, out var logItem))
                {
                    viewModel.CompleteProcessLog(logItem, $"Fehler: {ex.Message}");
                }
                else
                {
                    viewModel.AddProcessLog($"{fileName}: Fehler – {ex.Message}", isRunning: false);
                }
            });

        this.audioWatcher.Start();

        var mainWindow = new MainWindow(viewModel);
        mainWindow.Show();

        // ── Release Notes ─────────────────────────────────────────────────────
        var currentVersion = typeof(App).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";
        if (ReleaseNotesHelper.ShouldShow(persistedSettings.LastSeenReleaseNotesVersion, currentVersion))
        {
            var markdown = ReleaseNotesHelper.LoadMarkdown(typeof(App).Assembly);
            if (!string.IsNullOrWhiteSpace(markdown))
            {
                var html = ReleaseNotesHelper.RenderToHtml(markdown);
                var notesWindow = new ReleaseNotesWindow(html) { Owner = mainWindow };
                notesWindow.ShowDialog();
            }

            var updatedSettings = persistedSettings with { LastSeenReleaseNotesVersion = currentVersion };
            persistedSettingsHolder.Update(updatedSettings, persistedSettingsHolder.Prompts);
            runtimeSettingsHolder.Update(updatedSettings, runtimeSettingsHolder.Prompts);
            await settingsRepo.SaveAsync(updatedSettings);
        }

        _ = CheckForUpdatesAsync(crashLogger);
    }

    private static async Task CheckForUpdatesAsync(CrashLogWriter crashLogger)
    {
        try
        {
            // Updates werden vom Netzwerkpfad geprüft, in den das Build-Script die Releases kopiert.
            const string releasePath = @"Z:\12_Tools\Peano\Johann";
            if (!Directory.Exists(releasePath))
            {
                crashLogger.WriteCrashLog(
                    "UPDATE",
                    new DirectoryNotFoundException(
                        $"Update-Verzeichnis '{releasePath}' ist nicht erreichbar; Update-Prüfung übersprungen."));
                return;
            }

            var mgr = new UpdateManager(new SimpleFileSource(new DirectoryInfo(releasePath)));

            // A build started from source is not Velopack-installed. That is normal
            // during development and must stay quiet — everything else is a fault
            // and gets logged rather than swallowed (see #42).
            if (!mgr.IsInstalled)
            {
                return;
            }

            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null)
            {
                return;
            }

            var result = MessageBox.Show(
                $"Version {newVersion.TargetFullRelease.Version} ist verfügbar.\nJetzt herunterladen und neu starten?",
                "Platé.Johann – Update verfügbar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes)
            {
                return;
            }

            await mgr.DownloadUpdatesAsync(newVersion);
            mgr.ApplyUpdatesAndRestart(newVersion);
        }
        catch (Exception ex)
        {
            // Never swallow silently: this hid a total auto-update outage across
            // v1.1.0-v1.3.0 because nothing ever surfaced the locator error (#42).
            crashLogger.WriteCrashLog("UPDATE", ex);
        }
    }

    private static void EnsureEnvFile(string johannDir)
    {
        const string sourceEnv = @"X:\PRO_Programmierung\Peano.APP\APP17_Johann\Platee.Johann\.env";

        var targetEnv = Path.Combine(johannDir, ".env");
        if (File.Exists(targetEnv))
        {
            return;
        }

        var result = MessageBox.Show(
            "Die .env-Datei wurde nicht gefunden.\n\n" +
            "Diese Datei enthält den API-Schlüssel und wird für die KI-Verarbeitung benötigt.\n\n" +
            "Soll die Datei jetzt automatisch eingerichtet werden?",
            "Platé.Johann – Einrichtung erforderlich",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes)
        {
            return;
        }

        if (!File.Exists(sourceEnv))
        {
            MessageBox.Show(
                $"Die Quelldatei wurde nicht gefunden:\n{sourceEnv}\n\n" +
                "Bitte die .env-Datei manuell nach\n" +
                $"{targetEnv}\nkopieren.",
                "Platé.Johann – Datei nicht gefunden",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        Directory.CreateDirectory(johannDir);
        File.Copy(sourceEnv, targetEnv);

        MessageBox.Show(
            "Die .env-Datei wurde erfolgreich eingerichtet.",
            "Platé.Johann – Einrichtung abgeschlossen",
            MessageBoxButton.OK,
            MessageBoxImage.Information);
    }

    private static DirectoryValidationResult ValidateDirectory(string path)
    {
        try
        {
            Directory.CreateDirectory(path);
            return new DirectoryValidationResult(true);
        }
        catch (Exception ex)
        {
            return new DirectoryValidationResult(false, ex.Message);
        }
    }

    private static string ResolveDefaultOutputRoot()
    {
        // Default: Documents\Johann\output — independent of the Python project location
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Johann", "output");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string ResolveDefaultInputRoot()
    {
        // Default: Documents\Johann\Eingang
        var path = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
            "Johann", "Eingang");
        Directory.CreateDirectory(path);
        return path;
    }

    private static string DescribeFault(string label, Platee.Johann.Application.Settings.SettingsFileFault fault)
    {
        var backup = fault.BackupPath is null
            ? "Es konnte keine Sicherungskopie angelegt werden."
            : $"Eine Sicherungskopie liegt unter: {fault.BackupPath}";

        return string.Join(
            Environment.NewLine,
            $"{label}:",
            $"Datei: {fault.FilePath}",
            $"Grund: {fault.Reason}",
            backup);
    }

    private static string BuildSettingsFaultMessage(IReadOnlyList<string> faults)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Beim Start konnten nicht alle Einstellungen gelesen werden.");
        sb.AppendLine("Für diese Sitzung gelten Ersatzwerte.");
        sb.AppendLine();

        foreach (var fault in faults)
        {
            sb.AppendLine(fault);
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    private static string BuildPathWarningMessage(IReadOnlyList<StartupPathIssue> issues)
    {
        var sb = new StringBuilder();
        sb.AppendLine("Einige konfigurierte Verzeichnisse konnten beim Start nicht verwendet werden.");
        sb.AppendLine("Die gespeicherten Einstellungen wurden nicht geändert.");
        sb.AppendLine("Für diese Sitzung werden Ersatzpfade verwendet.");
        sb.AppendLine();

        foreach (var issue in issues)
        {
            sb.AppendLine($"{issue.Label}:");
            sb.AppendLine($"Gespeichert: {issue.ConfiguredPath}");
            sb.AppendLine($"Grund: {issue.Reason}");
            sb.AppendLine($"Verwendet: {issue.FallbackPath}");
            sb.AppendLine();
        }

        return sb.ToString().TrimEnd();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        this.DisposeServices();
    }

    private void DisposeServices()
    {
        // Called from both OnExit and the ProcessExit crash-path hook; Dispose is
        // idempotent. The LLM provider holds nothing disposable (see
        // OpenAiLlmProvider), so the watcher is the only resource to release.
        this.audioWatcher?.Dispose();
    }
}
