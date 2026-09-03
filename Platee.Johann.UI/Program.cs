namespace Platee.Johann.UI;

using Velopack;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Must run first: processes Velopack install/update/uninstall hooks AND
        // initialises the locator that UpdateManager needs to find the install.
        // Gating this on hook arguments broke auto-update for every normal launch
        // (a double-clicked shortcut passes none of them) — see #42.
        // On a non-installed build this is a no-op, so `dotnet run` is unaffected.
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
