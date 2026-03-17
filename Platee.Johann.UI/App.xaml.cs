using System.IO;
using System.Windows;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Parsing;
using Platee.Johann.Infrastructure.Json;
using Platee.Johann.Infrastructure.Llm;
using Platee.Johann.Infrastructure.Renderers;
using Platee.Johann.UI.ViewModels;

namespace Platee.Johann.UI;

public partial class App : System.Windows.Application
{
    private static readonly string CrashLog =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Johann_crash.txt");

    private AudioWatcherService? _audioWatcher;

    protected override void OnStartup(StartupEventArgs e)
    {
        DispatcherUnhandledException += (_, ex) =>
        {
            File.AppendAllText(CrashLog, $"[{DateTime.Now}] DISPATCHER: {ex.Exception}\n\n");
            ex.Handled = false;
        };
        AppDomain.CurrentDomain.UnhandledException += (_, ex) =>
        {
            File.AppendAllText(CrashLog, $"[{DateTime.Now}] UNHANDLED: {ex.ExceptionObject}\n\n");
        };
        TaskScheduler.UnobservedTaskException += (_, ex) =>
        {
            File.AppendAllText(CrashLog, $"[{DateTime.Now}] TASK: {ex.Exception}\n\n");
        };

        base.OnStartup(e);

        // ── Settings ──────────────────────────────────────────────────────────
        var settingsDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann");
        ISettingsRepository settingsRepo = new JsonSettingsRepository(settingsDir);

        // Load settings synchronously at startup using Task.Run to avoid UI thread deadlocks
        var initialSettings = Task.Run(() => settingsRepo.LoadAsync()).GetAwaiter().GetResult();

        // Locate the output directory.
        // Priority: 1. Ausgabeverzeichnis from Config, 2. CLI Argument, 3. Default fallback
        if (string.IsNullOrWhiteSpace(initialSettings.Ausgabeverzeichnis))
        {
            var newAusgabe = e.Args.Length > 0 && Directory.Exists(e.Args[0])
                ? e.Args[0]
                : ResolveDefaultOutputRoot();

            Directory.CreateDirectory(newAusgabe);
            initialSettings = initialSettings with { Ausgabeverzeichnis = newAusgabe };
        }

        if (string.IsNullOrWhiteSpace(initialSettings.Quellverzeichnis))
        {
            var newQuell = ResolveDefaultInputRoot();
            Directory.CreateDirectory(newQuell);
            initialSettings = initialSettings with { Quellverzeichnis = newQuell };
        }

        if (string.IsNullOrWhiteSpace(initialSettings.Archivverzeichnis) && !string.IsNullOrWhiteSpace(initialSettings.Quellverzeichnis))
        {
            var newArchiv = Path.Combine(initialSettings.Quellverzeichnis, "Archiv");
            Directory.CreateDirectory(newArchiv);
            initialSettings = initialSettings with { Archivverzeichnis = newArchiv };
        }

        var outputRoot = initialSettings.Ausgabeverzeichnis;

        // Force a save to ensure missing template fields (like directories or new prompts) are written to the JSON
        Task.Run(() => settingsRepo.SaveAsync(initialSettings)).GetAwaiter().GetResult();

        var settingsHolder = new SettingsHolder(initialSettings);

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
