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

    protected override void OnStartup(StartupEventArgs e)
    {
        var crashLogger = new CrashLogWriter(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            typeof(App).Assembly.GetName().Version?.ToString());
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
        var settingsFilePath = Path.Combine(settingsDir, "settings.json");
        ISettingsRepository settingsRepo = new JsonSettingsRepository(settingsDir);

        // Load settings synchronously at startup using Task.Run to avoid UI thread deadlocks
        var persistedSettings = Task.Run(() => settingsRepo.LoadAsync()).GetAwaiter().GetResult();

        // Clean up legacy local prompt files (one-time, idempotent)
        SettingsSplitMigration.CleanupLegacyFiles(settingsDir);

        // ── Prompt settings ───────────────────────────────────────────────────
        // Global prompt file is the single source of truth.
        // Falls back to built-in defaults if unreachable.
        var effectivePrompts = PromptSettings.Default;
        if (!string.IsNullOrWhiteSpace(persistedSettings.GlobalPromptFilePath))
        {
            var globalPromptRepo = JsonPromptSettingsRepository.FromFilePath(persistedSettings.GlobalPromptFilePath);
            if (globalPromptRepo.IsReachable)
            {
                effectivePrompts = Task.Run(() => globalPromptRepo.LoadAsync()).GetAwaiter().GetResult();
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

        var persistedSettingsHolder = new SettingsHolder(persistedSettings, effectivePrompts);
        var runtimeSettingsHolder = new SettingsHolder(effectiveSettings, effectivePrompts);

        // ── .env-Prüfung ──────────────────────────────────────────────────────
        EnsureEnvFile(settingsDir);

        // ── Manual DI ─────────────────────────────────────────────────────────
        IEntryRepository repository = new JsonRepository(outputRoot);

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
            outputRoot, overviewService, runtimeSettingsHolder, renderers);

        this.audioWatcher = new AudioWatcherService(processor, runtimeSettingsHolder);

        // ── Window ────────────────────────────────────────────────────────────
        var viewModel = new MainViewModel(repository, renderers, outputRoot, processor,
                                           settingsRepo, persistedSettingsHolder,
                                           runtimeSettingsHolder, pathResolution.Issues);

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
            persistedSettingsHolder.Current = updatedSettings;
            runtimeSettingsHolder.Current = updatedSettings;
            Task.Run(() => settingsRepo.SaveAsync(updatedSettings)).GetAwaiter().GetResult();
        }

        _ = CheckForUpdatesAsync();
    }

    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            // Updates werden vom Netzwerkpfad geprüft, in den das Build-Script die Releases kopiert.
            const string releasePath = @"Z:\12_Tools\Peano\Johann";
            if (!Directory.Exists(releasePath))
            {
                return;
            }

            var mgr = new UpdateManager(new SimpleFileSource(new DirectoryInfo(releasePath)));
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
        catch
        {
            // Nicht über Velopack installiert oder offline – still ignorieren
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
        this.audioWatcher?.Dispose();
    }
}
