namespace Platee.Johann.UiDriver.Automation;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Input;
using FlaUI.Core.Tools;
using FlaUI.UIA3;
using Platee.Johann.UiDriver.Sandbox;
using FlaUiApplication = FlaUI.Core.Application;

/// <summary>
/// Drives a real Johann process via FlaUI (UIA3): starts it against an isolated sandbox (or
/// attaches to an already-running one), finds elements by AutomationId/Name, and exposes the
/// handful of interactions the <c>ui-driver</c> console tool needs.
/// </summary>
public sealed class JohannSession : IDisposable
{
    private const string ProcessName = "Platee.Johann.UI";

    private readonly FlaUiApplication app;
    private readonly UIA3Automation automation;
    private Window? mainWindow;

    private JohannSession(FlaUiApplication app, UIA3Automation automation)
    {
        this.app = app;
        this.automation = automation;
    }

    public int ProcessId => this.app.ProcessId;

    public Window MainWindow => this.mainWindow
        ?? throw new InvalidOperationException("Kein Hauptfenster bekannt — Launch/Attach hat keines ermittelt.");

    public static JohannSession Launch(JohannLaunchOptions options, TimeSpan? timeout = null, bool expectDialogs = false)
    {
        var running = Process.GetProcessesByName(ProcessName);
        if (running.Length > 0)
        {
            throw new InvalidOperationException($"Johann läuft bereits (PID {running[0].Id}) — bitte schließen.");
        }

        // The sandbox guard runs before every start, no exceptions — a start against a
        // misconfigured or unsafe sandbox must never reach a real Johann process.
        SandboxGuard.Ensure(options.Sandbox);

        var psi = new ProcessStartInfo(options.ExePath)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(options.ExePath) ?? throw new InvalidOperationException($"Kein Verzeichnis für '{options.ExePath}' ermittelbar."),
        };

        psi.Environment["JOHANN_HOME"] = options.Sandbox.Home;
        psi.Environment["JOHANN_NO_UPDATE_CHECK"] = options.SkipUpdateCheck ? "1" : "0";

        if (options.OpenAiRoot is not null)
        {
            psi.Environment["JOHANN_OPENAI_ENDPOINT"] = options.OpenAiRoot.AbsoluteUri;
        }
        else
        {
            // Without an override, Johann must fall back to the real OpenAI endpoint on its own —
            // an endpoint inherited from the parent shell's environment must never leak in.
            psi.Environment.Remove("JOHANN_OPENAI_ENDPOINT");
        }

        if (options.ApiKey is not null)
        {
            psi.Environment["OPENAI_API_KEY"] = options.ApiKey;
        }
        else
        {
            // Audit: without an override, the key comes from the sandboxed .env — never leak the real one in.
            psi.Environment.Remove("OPENAI_API_KEY");
        }

        var app = FlaUiApplication.Launch(psi);
        var automation = new UIA3Automation();
        var session = new JohannSession(app, automation);
        try
        {
            session.mainWindow = session.WaitForMainWindow(timeout ?? TimeSpan.FromSeconds(30), expectDialogs);
        }
        catch
        {
            // A failed wait must not leave an orphan Johann process — it would block every later
            // Launch with "läuft bereits" until someone finds and kills it by hand. The screenshot
            // (if any) is already on disk by the time WaitForMainWindow throws.
            TryKill(app);
            automation.Dispose();
            app.Dispose();
            throw;
        }

        return session;
    }

    public static JohannSession Attach(int processId)
    {
        var app = FlaUiApplication.Attach(processId);
        var automation = new UIA3Automation();
        var session = new JohannSession(app, automation);
        session.mainWindow = session.Windows().FirstOrDefault(w => JohannWindows.IsMainWindow(w.Title))
            ?? session.Windows().FirstOrDefault();
        return session;
    }

    public static string ReadClipboard()
    {
        string? text = null;
        var thread = new Thread(() => text = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty);
        thread.SetApartmentState(ApartmentState.STA);
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException("Zwischenablage nicht lesbar (gesperrt?).");
        }

        return text ?? string.Empty;
    }

    public IReadOnlyList<Window> Windows() => this.app.GetAllTopLevelWindows(this.automation);

    public AutomationElement Find(string idOrName, TimeSpan? timeout = null, Window? scope = null)
    {
        if (scope is not null)
        {
            var result = Retry.WhileNull(
                () => scope.FindFirstDescendant(cf => cf.ByAutomationId(idOrName).Or(cf.ByName(idOrName))),
                timeout ?? TimeSpan.FromSeconds(5));

            return result.Result ?? throw new ElementNotFoundException(idOrName, this.Tree());
        }

        return this.FindWithWindow(idOrName, timeout).Element;
    }

    public AutomationElement? TryFind(string idOrName, TimeSpan? timeout = null)
    {
        try
        {
            return this.Find(idOrName, timeout);
        }
        catch (ElementNotFoundException)
        {
            return null;
        }
    }

    public void Click(string idOrName, bool mouse = false)
    {
        var element = this.Find(idOrName);
        if (mouse || !element.Patterns.Invoke.IsSupported)
        {
            // Buttons opening modal WPF dialogs must be clicked with the mouse — Invoke can block.
            element.Click();
        }
        else
        {
            element.Patterns.Invoke.Pattern.Invoke();
        }
    }

    public void RightClick(string idOrName) => this.Find(idOrName).RightClick();

    public void DoubleClick(string idOrName) => this.Find(idOrName).DoubleClick();

    public void Type(string idOrName, string text)
    {
        var element = this.Find(idOrName);
        if (element.Patterns.Value.IsSupported)
        {
            element.AsTextBox().Text = text;
        }
        else
        {
            element.Focus();
            Keyboard.Type(text);
        }
    }

    public void Key(string chord) => Keyboard.Type(KeyChord.Parse(chord));

    public string Screenshot(string path, string? idOrName = null)
    {
        Window window;
        AutomationElement element;
        if (idOrName is null)
        {
            window = this.MainWindow;
            element = window;
        }
        else
        {
            (window, element) = this.FindWithWindow(idOrName);
        }

        using var windowBitmap = JohannWindows.CaptureWindow(window);
        if (ReferenceEquals(element, window))
        {
            windowBitmap.Save(path, ImageFormat.Png);
        }
        else
        {
            using var cropped = JohannWindows.Crop(
                windowBitmap,
                window.Properties.BoundingRectangle.ValueOrDefault,
                element.Properties.BoundingRectangle.ValueOrDefault);
            cropped.Save(path, ImageFormat.Png);
        }

        return path;
    }

    public string Tree(string? idOrName = null, int depth = 8)
    {
        IEnumerable<AutomationElement> roots = idOrName is null
            ? this.Windows()
            : [this.Find(idOrName)];

        return UiTreeDump.ToJson(roots.Select(root => JohannWindows.BuildNode(root, depth)));
    }

    public void Close()
    {
        if (this.app.HasExited)
        {
            return;
        }

        try
        {
            this.MainWindow.Close();
        }
        catch (Exception)
        {
            // The window may already be gone (e.g. crashed) — fall through to the wait/kill below.
        }

        var deadline = DateTime.UtcNow + TimeSpan.FromSeconds(10);
        while (!this.app.HasExited && DateTime.UtcNow < deadline)
        {
            Thread.Sleep(100);
        }

        if (!this.app.HasExited)
        {
            this.app.Kill();
        }
    }

    public void Dispose()
    {
        this.Close();
        this.automation.Dispose();
        this.app.Dispose();
    }

    private static void TryKill(FlaUiApplication app)
    {
        try
        {
            if (!app.HasExited)
            {
                app.Kill();
            }
        }
        catch (Exception)
        {
            // Best-effort cleanup — the process may already be gone.
        }
    }

    /// <summary>
    /// Like <see cref="Find(string, TimeSpan?, Window?)"/> across all windows, but also returns
    /// which window the match came from — needed by <see cref="Screenshot"/> to capture the right
    /// window and crop relative to its bounds.
    /// </summary>
    private (Window Window, AutomationElement Element) FindWithWindow(string idOrName, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        do
        {
            foreach (var window in this.Windows())
            {
                var element = window.FindFirstDescendant(cf => cf.ByAutomationId(idOrName).Or(cf.ByName(idOrName)));
                if (element is not null)
                {
                    return (window, element);
                }
            }

            Thread.Sleep(100);
        }
        while (DateTime.UtcNow < deadline);

        throw new ElementNotFoundException(idOrName, this.Tree());
    }

    private Window WaitForMainWindow(TimeSpan timeout, bool expectDialogs)
    {
        var deadline = DateTime.UtcNow + timeout;
        while (DateTime.UtcNow < deadline)
        {
            if (this.app.HasExited)
            {
                throw new InvalidOperationException("Johann wurde beendet, bevor das Hauptfenster erschien.");
            }

            var windows = this.Windows();
            foreach (var window in windows)
            {
                if (expectDialogs || JohannWindows.IsMainWindow(window.Title))
                {
                    return window;
                }

                var screenshotPath = JohannWindows.TrySaveScreenshot(window, "unexpected");
                throw new UnexpectedWindowException(window.Title, screenshotPath);
            }

            Thread.Sleep(250);
        }

        throw new TimeoutException($"Hauptfenster von Johann nicht erschienen innerhalb von {timeout}.");
    }
}
