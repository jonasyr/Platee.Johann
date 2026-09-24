namespace Platee.Johann.UiDriver.Tool;

using System.Globalization;
using System.Text.Json;
using Platee.Johann.UiDriver.Audio;
using Platee.Johann.UiDriver.Automation;
using Platee.Johann.UiDriver.Sandbox;

/// <summary>
/// Dispatches the <c>ui-driver</c> command line. Every command prints exactly one line of JSON
/// to stdout; a failure prints <c>{"error": "..."}</c> and the process exits with code 1.
/// </summary>
public static class Commands
{
    private const string DefaultTeamPromptsPath = @"Z:\12_Tools\Peano\Johann\prompts.json";

    public static int Run(string[] args)
    {
        try
        {
            Console.WriteLine(Execute(args));
            return 0;
        }
        catch (Exception ex)
        {
            Console.WriteLine(JsonSerializer.Serialize(new { error = ex.Message }));
            return 1;
        }
    }

    private static string Execute(string[] args)
    {
        if (args.Length == 0)
        {
            throw new ArgumentException("Kein Befehl angegeben.");
        }

        var rest = args[1..];
        return args[0] switch
        {
            "sandbox" => Sandbox(rest),
            "start" => Start(rest),
            "silence" => Silence(rest),
            "windows" => Attach(session => Ok(new { windows = session.Windows().Select(w => w.Title).ToArray() })),
            "tree" => Attach(session => session.Tree(
                CommandArgs.Option(rest, "--of"),
                int.TryParse(CommandArgs.Option(rest, "--depth"), NumberStyles.Integer, CultureInfo.InvariantCulture, out var depth) ? depth : 8)),
            "click" => Attach(session =>
            {
                session.Click(FirstPositional(rest, "id|name"), CommandArgs.Flag(rest, "--mouse"));
                return Ok();
            }),
            "rightclick" => Attach(session =>
            {
                session.RightClick(FirstPositional(rest, "id|name"));
                return Ok();
            }),
            "doubleclick" => Attach(session =>
            {
                session.DoubleClick(FirstPositional(rest, "id|name"));
                return Ok();
            }),
            "type" => Attach(session =>
            {
                var positionals = CommandArgs.PositionalsExcluding(rest);
                session.Type(CommandArgs.RequireAt(positionals, 0, "id|name"), CommandArgs.RequireAt(positionals, 1, "text"));
                return Ok();
            }),
            "key" => Attach(session =>
            {
                session.Key(FirstPositional(rest, "chord"));
                return Ok();
            }),
            "screenshot" => Attach(session =>
            {
                var path = session.Screenshot(FirstPositional(rest, "file"), CommandArgs.Option(rest, "--of"));
                return Ok(new { path });
            }),
            "wait-for" => Attach(session =>
            {
                session.Find(FirstPositional(rest, "id|name"), ParseTimeout(CommandArgs.Option(rest, "--timeout")));
                return Ok(new { found = true });
            }),
            "clipboard" => Attach(_ => Ok(new { text = JohannSession.ReadClipboard() })),
            "close" => CloseSession(),
            _ => throw new ArgumentException($"Unbekannter Befehl '{args[0]}'."),
        };
    }

    private static string Sandbox(string[] args)
    {
        var subArgs = args[1..];
        return args.Length == 0 ? throw new ArgumentException("Fehlender Sandbox-Unterbefehl (new|check).") : args[0] switch
        {
            "new" => SandboxNew(subArgs),
            "check" => SandboxCheck(subArgs),
            _ => throw new ArgumentException($"Unbekannter Sandbox-Unterbefehl '{args[0]}'."),
        };
    }

    private static string SandboxNew(string[] args)
    {
        var root = CommandArgs.RequireOption(args, "--root");
        var from = CommandArgs.Option(args, "--from")
            ?? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann");
        var team = CommandArgs.Option(args, "--team") ?? DefaultTeamPromptsPath;

        var layout = AuditSandbox.Create(root, from, team);
        return Ok(new
        {
            root = layout.Root,
            home = layout.Home,
            output = layout.Output,
            eingang = layout.Eingang,
            archiv = layout.Archiv,
            teamPrompts = layout.TeamPrompts,
        });
    }

    private static string SandboxCheck(string[] args)
    {
        var layout = new SandboxLayout(CommandArgs.RequireOption(args, "--root"));
        SandboxGuard.Ensure(layout);
        return Ok();
    }

    private static string Start(string[] args)
    {
        var layout = new SandboxLayout(CommandArgs.RequireOption(args, "--root"));
        var exePath = CommandArgs.Option(args, "--exe") ?? DefaultExePath();
        var endpointRaw = CommandArgs.Option(args, "--endpoint");
        var endpoint = endpointRaw is null ? null : new Uri(endpointRaw);

        var options = new JohannLaunchOptions(exePath, layout, endpoint, ApiKey: null);
        var session = JohannSession.Launch(options);
        SessionStore.Save(session.ProcessId, layout.Root);
        return Ok(new { pid = session.ProcessId, root = layout.Root, mainWindow = session.MainWindow.Title });
    }

    private static string Silence(string[] args)
    {
        var minutes = double.Parse(CommandArgs.RequireOption(args, "--minutes"), CultureInfo.InvariantCulture);
        var positionals = CommandArgs.PositionalsExcluding(args, "--minutes");
        var path = CommandArgs.RequireAt(positionals, 0, "out.mp3");

        SilenceMp3.Write(path, TimeSpan.FromMinutes(minutes));
        return Ok(new { path });
    }

    private static string CloseSession()
    {
        var session = JohannSession.Attach(SessionStore.LoadRunningPid());
        session.Close();
        SessionStore.Clear();
        return Ok();
    }

    private static string Attach(Func<JohannSession, string> action) =>
        action(JohannSession.Attach(SessionStore.LoadRunningPid()));

    private static string FirstPositional(string[] args, string description) =>
        CommandArgs.RequireAt(CommandArgs.PositionalsExcluding(args), 0, description);

    private static TimeSpan? ParseTimeout(string? seconds) =>
        seconds is null ? null : TimeSpan.FromSeconds(double.Parse(seconds, CultureInfo.InvariantCulture));

    private static string Ok() => Ok(new { ok = true });

    private static string Ok(object payload) => JsonSerializer.Serialize(payload);

    private static string DefaultExePath()
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null && !File.Exists(Path.Combine(directory.FullName, "Platee.Johann.slnx")))
        {
            directory = directory.Parent;
        }

        var root = directory?.FullName
            ?? throw new InvalidOperationException("Repo-Wurzel (Platee.Johann.slnx) nicht gefunden — --exe angeben.");
        return Path.Combine(root, "Platee.Johann.UI", "bin", "Debug", "net10.0-windows", "Platee.Johann.UI.exe");
    }
}
