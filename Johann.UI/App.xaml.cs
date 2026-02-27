using System.IO;
using System.Windows;
using Johann.Infrastructure.Json;
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

        // Manual DI
        IEntryRepository repository = new JsonRepository(outputRoot);
        IEntryRenderer[] renderers  =
        [
            new PdfRenderer(),
            new HtmlRenderer(),
            new EmailRenderer(),
        ];

        var viewModel  = new MainViewModel(repository, renderers, outputRoot);
        var mainWindow = new MainWindow(viewModel);
        mainWindow.Show();
    }

    private static string ResolveDefaultOutputRoot()
    {
        var exeDir    = AppDomain.CurrentDomain.BaseDirectory;
        var candidate = Path.GetFullPath(Path.Combine(exeDir, "..", "..", "..", "..", "..", "output"));
        return Directory.Exists(candidate) ? candidate : Path.Combine(exeDir, "output");
    }
}
