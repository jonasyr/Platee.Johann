using System.IO;
using System.Windows;
using Platee.Johann.Application.Diagnostics;
using Platee.Johann.Application.Processing;
using Velopack;
using Velopack.Sources;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Parsing;
using Platee.Johann.Infrastructure.Json;
using Platee.Johann.Infrastructure.Llm;
using Platee.Johann.Infrastructure.Renderers;
using Platee.Johann.UI.ViewModels;

namespace Platee.Johann.UI;

public partial class App : System.Windows.Application
{
    private AudioWatcherService? _audioWatcher;

    protected override void OnStartup(StartupEventArgs e)
    {
        var crashLogger = new CrashLogWriter(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            typeof(App).Assembly.GetName().Version?.ToString());
        crashLogger.EnsureLogDirectory();

        DispatcherUnhandledException += (_, ex) =>
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
        var initialSettings = Task.Run(() => settingsRepo.LoadAsync()).GetAwaiter().GetResult();

        var promptMigration = PromptDefaultsMigration.ApplyIfNeeded(initialSettings, settingsFilePath);
        initialSettings = promptMigration.Settings;

        // Locate the output directory.
        // Priority: 1. Ausgabeverzeichnis from Config, 2. CLI Argument, 3. Default fallback
        // Falls back to default if the configured path cannot be created (e.g. path from a different machine).
        if (string.IsNullOrWhiteSpace(initialSettings.Ausgabeverzeichnis)
            || !TryEnsureDirectory(initialSettings.Ausgabeverzeichnis))
        {
            var newAusgabe = e.Args.Length > 0 && Directory.Exists(e.Args[0])
                ? e.Args[0]
                : ResolveDefaultOutputRoot();

            initialSettings = initialSettings with { Ausgabeverzeichnis = newAusgabe };
        }

        if (string.IsNullOrWhiteSpace(initialSettings.Quellverzeichnis)
            || !TryEnsureDirectory(initialSettings.Quellverzeichnis))
        {
            initialSettings = initialSettings with { Quellverzeichnis = ResolveDefaultInputRoot() };
        }

        if (string.IsNullOrWhiteSpace(initialSettings.Archivverzeichnis)
            || !TryEnsureDirectory(initialSettings.Archivverzeichnis))
        {
            var newArchiv = Path.Combine(initialSettings.Quellverzeichnis, "Archiv");
            Directory.CreateDirectory(newArchiv);
            initialSettings = initialSettings with { Archivverzeichnis = newArchiv };
        }

        var outputRoot = initialSettings.Ausgabeverzeichnis;

        // Force a save to ensure missing template fields (like directories or new prompts) are written to the JSON
        Task.Run(() => settingsRepo.SaveAsync(initialSettings)).GetAwaiter().GetResult();

        if (promptMigration.DidMigrate && promptMigration.BackupPath is not null)
        {
            MessageBox.Show(
                "Ihre individuellen Prompts wurden mit den neuen Defaults überschrieben.\n\n" +
                $"Die ursprünglichen Einstellungen wurden gesichert unter:\n{promptMigration.BackupPath}",
                "Platé.Johann – Prompts aktualisiert",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        var settingsHolder = new SettingsHolder(initialSettings);

        // ── .env-Prüfung ──────────────────────────────────────────────────────
        EnsureEnvFile(settingsDir);

        // ── Manual DI ─────────────────────────────────────────────────────────
        IEntryRepository repository = new JsonRepository(outputRoot);

        // HTML overview service — regenerates _ItemÜbersicht.html after every save
        IHtmlOverviewService overviewService = new HtmlOverviewService(repository, outputRoot);

        IEntryRenderer[] renderers =
        [
            new PdfRenderer(settingsHolder),
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

        var summaryGenerator = new SummaryGenerator(llmProvider, settingsHolder);
        IEntryProcessor processor = new EntryProcessingService(
            transcriber, summaryGenerator, new HeaderParser(), repository,
            outputRoot, overviewService, settingsHolder, renderers);

        _audioWatcher = new AudioWatcherService(processor, settingsHolder);

        // ── Window ────────────────────────────────────────────────────────────
        var viewModel = new MainViewModel(repository, renderers, outputRoot, processor,
                                           settingsRepo, settingsHolder);

        // Track per-file log items for the watcher
        var watcherLogs = new System.Collections.Concurrent.ConcurrentDictionary<string, ProcessLogItem>();

        _audioWatcher.EntryProcessingProgress += (filePath, progress) =>
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

        _audioWatcher.EntryProcessed += (filePath, entry) =>
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                viewModel.NotifyEntryProcessed(entry);
                if (watcherLogs.TryRemove(filePath, out var logItem))
                    viewModel.CompleteProcessLog(logItem, $"✓ {entry.Title}");
            });

        _audioWatcher.EntryProcessingFailed += (filePath, ex) =>
            System.Windows.Application.Current.Dispatcher.InvokeAsync(() =>
            {
                var fileName = Path.GetFileName(filePath);
                if (watcherLogs.TryRemove(filePath, out var logItem))
                    viewModel.CompleteProcessLog(logItem, $"Fehler: {ex.Message}");
                else
                    viewModel.AddProcessLog($"{fileName}: Fehler – {ex.Message}", isRunning: false);
            });

        _audioWatcher.Start();

        var mainWindow = new MainWindow(viewModel);
        mainWindow.Show();

        _ = CheckForUpdatesAsync();
    }

    private static async Task CheckForUpdatesAsync()
    {
        try
        {
            // Updates werden vom Netzwerkpfad geprüft, in den das Build-Script die Releases kopiert.
            const string releasePath = @"Z:\12_Tools\Peano\Johann";
            if (!Directory.Exists(releasePath)) return;
            var mgr = new UpdateManager(new SimpleFileSource(new DirectoryInfo(releasePath)));
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null) return;

            var result = MessageBox.Show(
                $"Version {newVersion.TargetFullRelease.Version} ist verfügbar.\nJetzt herunterladen und neu starten?",
                "Platé.Johann – Update verfügbar",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information);

            if (result != MessageBoxResult.Yes) return;

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
        if (File.Exists(targetEnv)) return;

        var result = MessageBox.Show(
            "Die .env-Datei wurde nicht gefunden.\n\n" +
            "Diese Datei enthält den API-Schlüssel und wird für die KI-Verarbeitung benötigt.\n\n" +
            "Soll die Datei jetzt automatisch eingerichtet werden?",
            "Platé.Johann – Einrichtung erforderlich",
            MessageBoxButton.YesNo,
            MessageBoxImage.Question);

        if (result != MessageBoxResult.Yes) return;

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

    private static bool TryEnsureDirectory(string path)
    {
        try { Directory.CreateDirectory(path); return true; }
        catch { return false; }
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

    protected override void OnExit(ExitEventArgs e)
    {
        base.OnExit(e);
        _audioWatcher?.Dispose();
    }
}
