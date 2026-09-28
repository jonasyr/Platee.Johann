namespace Platee.Johann.UiDriver.Automation;

using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Windows.Forms;
using FlaUI.Core.AutomationElements;
using FlaUI.Core.Definitions;
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

    // The first start on a fresh CI runner (cold disk, first JIT) took over 30 s; locally it is
    // a few seconds. Only an upper bound — a hang still fails, just later.
    private static readonly TimeSpan MainWindowTimeout = TimeSpan.FromSeconds(90);

    private readonly FlaUiApplication app;
    private readonly UIA3Automation automation;
    private Window? mainWindow;

    static JohannSession()
    {
        // Must run before any UIA/Win32 pixel-based call — GetWindowRect/PrintWindow and UIA's
        // BoundingRectangle must agree on physical vs. logical pixels (see DpiAwareness). Running
        // this from the static constructor also covers the xUnit test host, which never sees an
        // app.manifest.
        DpiAwareness.EnsurePerMonitorV2();
    }

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
        // Grace period, not an immediate throw: the previous test's own Close()/Dispose() allows
        // up to 10s to shut Johann down, and under CPU load that ordinary handover can still be in
        // flight when the next test's Launch() runs. A genuinely already-running Johann (someone
        // else has it open) still exhausts the loop below and throws.
        if (!TryWaitForNoRunningInstance(TimeSpan.FromSeconds(5), out var stillRunningPid))
        {
            throw new InvalidOperationException($"Johann läuft bereits (PID {stillRunningPid}) — bitte schließen.");
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
            session.mainWindow = session.WaitForMainWindow(timeout ?? MainWindowTimeout, expectDialogs);
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
        Exception? error = null;

        // The exception is carried out of the STA thread: thrown there, an unhandled
        // CLIPBRD_E_CANT_OPEN (another process holds the clipboard) crashed the whole test host
        // (found live, Task 12 fix round 1) instead of reaching the caller's retry.
        var thread = new Thread(() =>
        {
            try
            {
                text = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);

        // A clipboard call that hangs (another process holds it) must not keep the test host alive.
        thread.IsBackground = true;
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException("Zwischenablage nicht lesbar (gesperrt?).");
        }

        if (error is not null)
        {
            throw new InvalidOperationException("Zwischenablage nicht lesbar.", error);
        }

        return text ?? string.Empty;
    }

    /// <summary>
    /// Puts <paramref name="text"/> on the clipboard — a test sets a sentinel before a copy
    /// action so it can wait for the clipboard to change instead of reading it once, because a
    /// UIA Invoke on a WPF button runs the command asynchronously on the app's dispatcher.
    /// </summary>
    public static void WriteClipboard(string text)
    {
        Exception? error = null;
        var thread = new Thread(() =>
        {
            try
            {
                Clipboard.SetText(text);
            }
            catch (Exception ex)
            {
                error = ex;
            }
        });
        thread.SetApartmentState(ApartmentState.STA);

        // A clipboard call that hangs (another process holds it) must not keep the test host alive.
        thread.IsBackground = true;
        thread.Start();
        if (!thread.Join(TimeSpan.FromSeconds(5)))
        {
            throw new InvalidOperationException("Zwischenablage nicht beschreibbar (gesperrt?).");
        }

        if (error is not null)
        {
            throw new InvalidOperationException("Zwischenablage nicht beschreibbar.", error);
        }
    }

    /// <summary>
    /// Top-level windows plus every <c>ControlType.Window</c> descendant of them, de-duplicated by
    /// native handle. WPF modal dialogs with an <c>Owner</c> (e.g. release notes, a delete
    /// confirmation, an owned settings window) are exposed by UIA as Window-typed CHILDREN of the
    /// owning window, not as top-level windows in their own right — without this, they are
    /// invisible to <see cref="Find"/>/<see cref="Screenshot"/>/<c>windows</c> even though a plain
    /// Win32 <c>EnumWindows</c> shows them. Main window(s) come first, then each dialog in the
    /// order the tree walk finds it.
    /// </summary>
    public IReadOnlyList<Window> Windows()
    {
        var topLevel = this.app.GetAllTopLevelWindows(this.automation);
        var discovered = topLevel.Concat(topLevel.SelectMany(window =>
            window.FindAllDescendants(cf => cf.ByControlType(ControlType.Window)).Select(d => d.AsWindow())));

        return JohannWindows.DeduplicateByHandle(discovered, w => w.Properties.NativeWindowHandle.ValueOrDefault);
    }

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
        var (window, element) = this.FindWithWindow(idOrName);
        if (mouse || !element.Patterns.Invoke.IsSupported)
        {
            // Buttons opening modal WPF dialogs must be clicked with the mouse — Invoke can block.
            this.EnsureSafeToClick(window, element);
            element.Click();
        }
        else
        {
            // The Invoke pattern asks the element to invoke itself via UIA, not a simulated mouse
            // click — it never touches the foreground window or the cursor, so none of the
            // foreground/click-point safety checks below apply to it.
            element.Patterns.Invoke.Pattern.Invoke();
        }
    }

    /// <summary>
    /// Clicks a button of a Windows message box (Ja/Nein, Yes/No …) by its fixed control id
    /// instead of its caption, which depends on the Windows display language — the CI runner is
    /// English, so a search for "Ja" failed there. Only Win32 buttons match, never a WPF text that
    /// happens to be named "6".
    /// </summary>
    public void ClickDialogButton(DialogButton button, TimeSpan? timeout = null)
    {
        var id = ((int)button).ToString(System.Globalization.CultureInfo.InvariantCulture);
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        do
        {
            foreach (var window in this.Windows())
            {
                var element = window.FindFirstDescendant(cf =>
                    cf.ByAutomationId(id).And(cf.ByControlType(ControlType.Button)).And(cf.ByClassName("Button")));
                if (element is not null)
                {
                    this.EnsureSafeToClick(window, element);
                    element.Click();
                    return;
                }
            }

            Thread.Sleep(100);
        }
        while (DateTime.UtcNow < deadline);

        throw new ElementNotFoundException($"Dialogknopf {button} (Id {id})", this.Tree());
    }

    public void RightClick(string idOrName)
    {
        var (window, element) = this.FindWithWindow(idOrName);
        this.EnsureSafeToClick(window, element);
        element.RightClick();
    }

    public void DoubleClick(string idOrName)
    {
        var (window, element) = this.FindWithWindow(idOrName);
        this.EnsureSafeToClick(window, element);
        element.DoubleClick();
    }

    public void Type(string idOrName, string text)
    {
        var (window, element) = this.FindWithWindow(idOrName);
        if (element.Patterns.Value.IsSupported)
        {
            // Sets the value via UIA's Value pattern — programmatic, not simulated keystrokes, so
            // it needs no foreground switch.
            element.AsTextBox().Text = text;
        }
        else
        {
            InputSafety.EnsureForeground(window, this.ProcessId);
            element.Focus();
            Keyboard.Type(text);
        }
    }

    /// <summary>
    /// Sends a key chord to the resolved <see cref="KeyTarget"/> — an open modal Johann dialog if
    /// one exists, else the main window, or the window named by <paramref name="window"/> when
    /// given explicitly (e.g. <c>--window "Einstellungen – Platé.Johann"</c> for the non-modal
    /// Settings window, which is never picked automatically since it would otherwise steal
    /// keystrokes meant for the main window's own <c>InputBindings</c>, e.g. <c>Ctrl+0</c>).
    /// </summary>
    public void Key(string chord, string? window = null)
    {
        var keys = KeyChord.Parse(chord);
        var target = this.KeyTarget(window);
        InputSafety.EnsureForeground(target, this.ProcessId);
        NativeKeyboard.SendChord(keys);
    }

    /// <summary>
    /// The AutomationId of the element that currently has keyboard focus (empty when it has
    /// none), or <c>null</c> when nothing is focused or UIA cannot read it — the keyboard test
    /// (Task 13) collects these after each Tab.
    /// </summary>
    public string? FocusedAutomationId()
    {
        try
        {
            return this.automation.FocusedElement()?.Properties.AutomationId.ValueOrDefault;
        }
        catch (Exception)
        {
            // A focus change mid-read (the element is gone) is not an error for a caller that
            // just polls again after the next key.
            return null;
        }
    }

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

    /// <summary>
    /// Reads what <see cref="JohannWindows.SelectKeyTargetIndex"/> needs without ever throwing.
    /// Untitled windows (WPF popups/tooltips) are skipped before any pattern is touched: they
    /// lack the UIA Window pattern, and FlaUI's <c>Window.IsModal</c> then throws "The requested
    /// pattern 'Window' is not supported" — which aborted every <c>key</c> command live (S7).
    /// </summary>
    private static (string Title, bool IsModal, bool IsMain) KeyTargetCandidate(Window window)
    {
        var title = TitleOf(window);
        var isModal = title.Length > 0 && IsModalOf(window);
        return (title, isModal, JohannWindows.IsMainWindow(title));
    }

    private static string TitleOf(Window window) => window.Properties.Name.ValueOrDefault ?? string.Empty;

    private static bool IsModalOf(Window window) =>
        window.Patterns.Window.PatternOrDefault is { } pattern && pattern.IsModal.ValueOrDefault;

    private static bool MatchesWindow(Window window, string idOrName) =>
        string.Equals(window.Properties.AutomationId.ValueOrDefault, idOrName, StringComparison.Ordinal)
        || string.Equals(TitleOf(window), idOrName, StringComparison.Ordinal);

    /// <summary>
    /// Polls <see cref="Process.GetProcessesByName(string)"/> for up to <paramref name="graceTimeout"/>
    /// until no <c>Platee.Johann.UI</c> process remains. Every returned <see cref="Process"/> is
    /// disposed each iteration regardless of outcome — the array itself is not disposable, but its
    /// elements are.
    /// </summary>
    private static bool TryWaitForNoRunningInstance(TimeSpan graceTimeout, out int? stillRunningPid)
    {
        var deadline = DateTime.UtcNow + graceTimeout;
        while (true)
        {
            var running = Process.GetProcessesByName(ProcessName);
            try
            {
                if (running.Length == 0)
                {
                    stillRunningPid = null;
                    return true;
                }

                if (DateTime.UtcNow >= deadline)
                {
                    stillRunningPid = running[0].Id;
                    return false;
                }
            }
            finally
            {
                foreach (var process in running)
                {
                    process.Dispose();
                }
            }

            Thread.Sleep(200);
        }
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
    /// Resolves the window untargeted keyboard input (<see cref="Key"/>) should go to: the window
    /// named by <paramref name="explicitTitle"/> when given, else an open MODAL Johann dialog if
    /// one has a real title, else the main window. See
    /// <see cref="JohannWindows.SelectKeyTargetIndex"/> for the pure selection rule and why a
    /// non-modal dialog (Settings) and untitled windows (popups/tooltips) are never picked
    /// automatically.
    /// </summary>
    private Window KeyTarget(string? explicitTitle)
    {
        var windows = this.Windows();
        if (explicitTitle is not null)
        {
            return windows.FirstOrDefault(w => MatchesWindow(w, explicitTitle))
                ?? throw new ElementNotFoundException(explicitTitle, this.Tree());
        }

        var candidates = windows.Select(KeyTargetCandidate).ToArray();
        var index = JohannWindows.SelectKeyTargetIndex(candidates);
        return index >= 0 ? windows[index] : this.MainWindow;
    }

    /// <summary>
    /// Brings <paramref name="window"/> to the foreground and, when the element has a clickable
    /// point, verifies that point actually belongs to this session's process before a mouse click
    /// is sent — see <see cref="InputSafety"/> for why both checks exist and are separate from
    /// Invoke-pattern clicks, which never touch the foreground window or the cursor.
    /// </summary>
    private void EnsureSafeToClick(Window window, AutomationElement element)
    {
        InputSafety.EnsureForeground(window, this.ProcessId);

        if (element.TryGetClickablePoint(out var point))
        {
            InputSafety.EnsureClickPointBelongsToProcess(point, this.ProcessId);
        }
    }

    /// <summary>
    /// Like <see cref="Find(string, TimeSpan?, Window?)"/> across all windows, but also returns
    /// which window the match came from — needed by <see cref="Screenshot"/> to capture the right
    /// window and crop relative to its bounds. A window matching <paramref name="idOrName"/> by
    /// its own AutomationId/title wins over a descendant match in *any* window — this is what lets
    /// <c>screenshot --of "Platé.Johann – Neuigkeiten"</c> capture that dialog's own HWND instead
    /// of a crop of whichever window happened to expose it as a descendant.
    /// </summary>
    private (Window Window, AutomationElement Element) FindWithWindow(string idOrName, TimeSpan? timeout = null)
    {
        var deadline = DateTime.UtcNow + (timeout ?? TimeSpan.FromSeconds(5));
        do
        {
            var windows = this.Windows();

            var windowItself = windows.FirstOrDefault(w => MatchesWindow(w, idOrName));
            if (windowItself is not null)
            {
                return (windowItself, windowItself);
            }

            foreach (var window in windows)
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
