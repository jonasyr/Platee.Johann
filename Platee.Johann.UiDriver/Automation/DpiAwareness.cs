namespace Platee.Johann.UiDriver.Automation;

using System.Runtime.InteropServices;

/// <summary>
/// Makes the current process per-monitor DPI aware (v2), once, before any UIA or Win32
/// pixel-based call runs. Without this, on a scaled display <c>GetWindowRect</c>/<c>PrintWindow</c>
/// report physical pixels while UIA's <c>BoundingRectangle</c> (and everything computed from it —
/// screenshot sizing, crop math, click coordinates) reports DPI-virtualized logical coordinates.
/// At 125% scaling that showed up as a 1500x875 physical window captured as a 1200x700 PNG with
/// the right/bottom edge cut off.
/// </summary>
public static class DpiAwareness
{
    private static readonly IntPtr PerMonitorAwareV2 = new(-4);
    private static readonly object Gate = new();
    private static bool applied;

    /// <summary>
    /// Applies <c>DPI_AWARENESS_CONTEXT_PER_MONITOR_AWARE_V2</c> to the current process. Safe to
    /// call more than once (only the first call does anything) and safe to call when an
    /// app.manifest already set per-monitor-v2 awareness (the API then simply reports it cannot
    /// change an already-set context, which is not a failure worth surfacing here).
    /// </summary>
    public static void EnsurePerMonitorV2()
    {
        lock (Gate)
        {
            if (applied)
            {
                return;
            }

            try
            {
                SetProcessDpiAwarenessContext(PerMonitorAwareV2);
            }
            catch (Exception)
            {
                // Best-effort: an older OS may lack this API entirely — the process then stays at
                // whatever awareness it already had, which must not stop the tool from running.
            }
            finally
            {
                applied = true;
            }
        }
    }

    [DllImport("user32.dll", SetLastError = true)]
    private static extern bool SetProcessDpiAwarenessContext(IntPtr dpiContext);
}
