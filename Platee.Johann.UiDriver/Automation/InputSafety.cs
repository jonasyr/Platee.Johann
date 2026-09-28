namespace Platee.Johann.UiDriver.Automation;

using System.Drawing;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;

/// <summary>
/// Keyboard and mouse input sent by <c>ui-driver</c> (<see cref="Keyboard"/> keystrokes and
/// <see cref="AutomationElement.Click"/>/<c>RightClick</c>/<c>DoubleClick</c>) goes to whatever
/// window is actually in the foreground / under the cursor — not to whichever <see cref="Window"/>
/// object our code happens to be holding. Live: after Johann opened a PDF in PDF24, a
/// <c>Ctrl+0</c> zoom shortcut and a mouse click both landed in PDF24 instead of Johann, because a
/// plain <c>SetForegroundWindow</c> from a background process is refused by Windows' foreground
/// lock. This class makes that failure loud (an exception, before any input is sent) instead of
/// silent (input landing in the wrong app).
/// </summary>
internal static class InputSafety
{
    private const uint GaRootOwner = 3;
    private static readonly TimeSpan RetryBudget = TimeSpan.FromSeconds(1);
    private static readonly TimeSpan RetryInterval = TimeSpan.FromMilliseconds(50);

    /// <summary>
    /// Pure decision: is it safe to send input meant for <paramref name="targetProcessId"/> right
    /// now, given which process currently owns the foreground window?.
    /// </summary>
    public static bool IsForegroundSafe(int foregroundProcessId, int targetProcessId) =>
        foregroundProcessId != 0 && foregroundProcessId == targetProcessId;

    /// <summary>
    /// Brings <paramref name="window"/> to the foreground and verifies it — first via FlaUI's own
    /// <c>SetForeground</c>/<c>Focus</c> (which already tries the AttachThreadInput/ALT-key trick),
    /// then, if that was not enough, via a manual AttachThreadInput + BringWindowToTop +
    /// SetForegroundWindow sequence. Retries for about a second; throws instead of ever sending
    /// input to the wrong window.
    /// </summary>
    public static void EnsureForeground(Window window, int targetProcessId)
    {
        if (IsForegroundSafe(CurrentForegroundProcessId(), targetProcessId))
        {
            return;
        }

        var handle = window.Properties.NativeWindowHandle.ValueOrDefault;
        var deadline = DateTime.UtcNow + RetryBudget;
        do
        {
            TryBringToForeground(window, handle);
            if (IsForegroundSafe(CurrentForegroundProcessId(), targetProcessId))
            {
                return;
            }

            Thread.Sleep(RetryInterval);
        }
        while (DateTime.UtcNow < deadline);

        throw new InvalidOperationException("Johann ließ sich nicht in den Vordergrund holen – Eingabe abgebrochen");
    }

    /// <summary>
    /// Verifies the window at a screen point (root owner, so a click on a child control still
    /// resolves to its top-level window) belongs to <paramref name="targetProcessId"/>. Throws
    /// instead of clicking when another window (occluding Johann, or an unrelated app under the
    /// cursor after a failed foreground switch) actually owns that point.
    /// </summary>
    public static void EnsureClickPointBelongsToProcess(Point point, int targetProcessId)
    {
        if (ProcessIdAtPoint(point) != targetProcessId)
        {
            throw new InvalidOperationException("Zielfenster am Klickpunkt gehört nicht zu Johann – Eingabe abgebrochen");
        }
    }

    private static void TryBringToForeground(Window window, IntPtr handle)
    {
        try
        {
            window.SetForeground();
        }
        catch (Exception)
        {
            // FlaUI's own attempt can throw on a locked foreground — fall through to the manual
            // Win32 sequence below rather than giving up.
        }

        if (handle == IntPtr.Zero)
        {
            return;
        }

        var foregroundWindow = GetForegroundWindow();
        var foregroundThread = GetWindowThreadProcessId(foregroundWindow, out _);
        var ourThread = GetCurrentThreadId();

        var attached = foregroundThread != 0
            && foregroundThread != ourThread
            && AttachThreadInput(ourThread, foregroundThread, true);
        try
        {
            BringWindowToTop(handle);
            SetForegroundWindow(handle);
        }
        finally
        {
            if (attached)
            {
                AttachThreadInput(ourThread, foregroundThread, false);
            }
        }
    }

    private static int CurrentForegroundProcessId()
    {
        var hwnd = GetForegroundWindow();
        if (hwnd == IntPtr.Zero)
        {
            return 0;
        }

        GetWindowThreadProcessId(hwnd, out var processId);
        return (int)processId;
    }

    private static int? ProcessIdAtPoint(Point point)
    {
        var hwnd = WindowFromPoint(new NativePoint { X = point.X, Y = point.Y });
        if (hwnd == IntPtr.Zero)
        {
            return null;
        }

        var owner = GetAncestor(hwnd, GaRootOwner);
        GetWindowThreadProcessId(owner != IntPtr.Zero ? owner : hwnd, out var processId);
        return processId == 0 ? null : (int)processId;
    }

    [DllImport("user32.dll")]
    private static extern IntPtr GetForegroundWindow();

    [DllImport("user32.dll")]
    private static extern bool SetForegroundWindow(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern bool BringWindowToTop(IntPtr hWnd);

    [DllImport("user32.dll")]
    private static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint processId);

    [DllImport("user32.dll")]
    private static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool fAttach);

    [DllImport("kernel32.dll")]
    private static extern uint GetCurrentThreadId();

    [DllImport("user32.dll")]
    private static extern IntPtr WindowFromPoint(NativePoint point);

    [DllImport("user32.dll")]
    private static extern IntPtr GetAncestor(IntPtr hWnd, uint gaFlags);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativePoint
    {
        public int X;
        public int Y;
    }
}
