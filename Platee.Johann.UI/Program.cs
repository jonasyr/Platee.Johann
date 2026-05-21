namespace Platee.Johann.UI;

using Velopack;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Only run Velopack bootstrap when the process was actually started by a
        // Velopack hook / restart path. Plain `dotnet run` and direct exe launches
        // should continue straight into the WPF app during development.
        if (ShouldRunVelopackBootstrap(args))
        {
            VelopackApp.Build().Run();
        }

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }

    private static bool ShouldRunVelopackBootstrap(string[] args)
    {
        if (args.Any(a => a.StartsWith("--veloapp-", StringComparison.OrdinalIgnoreCase)))
        {
            return true;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("VELOPACK_FIRSTRUN"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        if (string.Equals(Environment.GetEnvironmentVariable("VELOPACK_RESTART"), "true", StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        return false;
    }
}
