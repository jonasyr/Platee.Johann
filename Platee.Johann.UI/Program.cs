using Velopack;

namespace Platee.Johann.UI;

public class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        // Muss als allererstes laufen – verarbeitet Velopack-Install/Update/Uninstall-Hooks
        VelopackApp.Build().Run();

        var app = new App();
        app.InitializeComponent();
        app.Run();
    }
}
