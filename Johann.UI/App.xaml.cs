using System.IO;
using System.Windows;
using Johann.Infrastructure.Json;
using Johann.UI.ViewModels;

namespace Johann.UI;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // Locate the output directory.
        // Override via first command-line argument: Johann.UI.exe "C:\path\to\output"
        var outputRoot = e.Args.Length > 0 && Directory.Exists(e.Args[0])
            ? e.Args[0]
            : ResolveDefaultOutputRoot();

        // Manual DI (Phase 1)
        IEntryRepository repository = new JsonRepository(outputRoot);
        IEntryRenderer[] renderers  = []; // Phase 2 will populate

        var viewModel  = new MainViewModel(repository, renderers);
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
