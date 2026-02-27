using System.IO;
using System.Windows;
using Johann.Application.Processing;
using Johann.Application.Settings;
using Johann.Domain.Parsing;
using Johann.Infrastructure.Json;
using Johann.Infrastructure.Llm;
using Johann.Infrastructure.Renderers;
using Johann.UI.ViewModels;

namespace Johann.UI;

public partial class App : System.Windows.Application
{
    private static readonly string CrashLog =
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.Desktop), "Johann_crash.txt");

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

        // Locate the output directory.
        // Override via first command-line argument: Johann.UI.exe "C:\path\to\output"
        var outputRoot = e.Args.Length > 0 && Directory.Exists(e.Args[0])
            ? e.Args[0]
            : ResolveDefaultOutputRoot();

        // ── Settings ──────────────────────────────────────────────────────────
        var settingsDir  = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann");
        ISettingsRepository settingsRepo = new JsonSettingsRepository(settingsDir);

        // Load settings synchronously at startup (small file, safe)
        var initialSettings  = settingsRepo.LoadAsync().GetAwaiter().GetResult();
        var settingsHolder   = new SettingsHolder(initialSettings);

        // ── Manual DI ─────────────────────────────────────────────────────────
        IEntryRepository repository = new JsonRepository(outputRoot);

        IEntryRenderer[] renderers =
        [
            new PdfRenderer(),
            new HtmlRenderer(),
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
            transcriber, summaryGenerator, new HeaderParser(), repository, outputRoot);

        // ── Window ────────────────────────────────────────────────────────────
        var viewModel  = new MainViewModel(repository, renderers, outputRoot, processor,
                                           settingsRepo, settingsHolder);
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
}
