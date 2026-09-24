namespace Platee.Johann.UiDriver.Automation;

using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using FlaUI.Core.AutomationElements;

/// <summary>
/// Small pure/IO helpers split out of <see cref="JohannSession"/> to keep it focused: the
/// main-window title rule, window/element screenshot capture, and UI-tree traversal.
/// </summary>
internal static class JohannWindows
{
    private const string MainWindowTitlePrefix = "Platé.Johann ";
    private const string SubWindowSeparator = " – ";
    private const uint PwRenderFullContent = 0x2;

    /// <summary>
    /// The main window's title is "Platé.Johann {version}" (see MainWindow.xaml). Every other
    /// Johann window/dialog is titled "Platé.Johann – …", so a title that starts with the plain
    /// prefix but does not contain the en-dash separator identifies the main window.
    /// </summary>
    public static bool IsMainWindow(string? title) =>
        title is not null
        && title.StartsWith(MainWindowTitlePrefix, StringComparison.Ordinal)
        && !title.Contains(SubWindowSeparator, StringComparison.Ordinal);

    public static string? TrySaveScreenshot(Window window, string prefix)
    {
        try
        {
            return SaveScreenshot(window, prefix);
        }
        catch (Exception)
        {
            // A screenshot is diagnostic best-effort; losing it must not hide the real failure.
            return null;
        }
    }

    public static string SaveScreenshot(Window window, string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), "johann-ui-driver");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.png");
        using var bitmap = CaptureWindow(window);
        bitmap.Save(path, ImageFormat.Png);
        return path;
    }

    /// <summary>
    /// Captures the window itself via <c>PrintWindow</c> (works even when another window occludes
    /// it, and never steals focus) instead of a screen-region grab, which would capture whatever
    /// currently covers Johann on screen. Falls back to FlaUI's own (screen-region) capture only
    /// when <c>PrintWindow</c> fails, e.g. no native handle or an unsupported window type.
    /// </summary>
    public static Bitmap CaptureWindow(Window window)
    {
        var handle = window.Properties.NativeWindowHandle.ValueOrDefault;
        if (handle != IntPtr.Zero && TryCapturePrintWindow(handle, out var bitmap) && bitmap is not null)
        {
            return bitmap;
        }

        return window.Capture();
    }

    /// <summary>
    /// Crops a captured window bitmap to one element's bounds, both rectangles being UIA
    /// BoundingRectangles in the same (screen) coordinate space.
    /// </summary>
    public static Bitmap Crop(Bitmap windowBitmap, Rectangle windowBounds, Rectangle elementBounds)
    {
        var relative = new Rectangle(
            elementBounds.X - windowBounds.X,
            elementBounds.Y - windowBounds.Y,
            elementBounds.Width,
            elementBounds.Height);

        // Clamp to the captured bitmap — UIA bounds can be stale by a pixel or two, or the window
        // rect we captured against may not exactly match the one the element bounds were read from.
        relative.Intersect(new Rectangle(0, 0, windowBitmap.Width, windowBitmap.Height));
        if (relative.Width <= 0 || relative.Height <= 0)
        {
            return (Bitmap)windowBitmap.Clone();
        }

        return windowBitmap.Clone(relative, windowBitmap.PixelFormat);
    }

    /// <summary>
    /// Removes duplicates by native window handle, keeping the first occurrence of each handle —
    /// callers put top-level windows first and their <c>Window</c>-typed descendants after, so the
    /// main window/each dialog's own entry wins over ever finding it again while walking the tree.
    /// A handle of <see cref="IntPtr.Zero"/> (no native window, or the property is unsupported) is
    /// never treated as a duplicate of another zero handle — there is nothing to de-dup by.
    /// </summary>
    public static IReadOnlyList<T> DeduplicateByHandle<T>(IEnumerable<T> items, Func<T, IntPtr> handleOf)
    {
        var seen = new HashSet<IntPtr>();
        var result = new List<T>();
        foreach (var item in items)
        {
            var handle = handleOf(item);
            if (handle != IntPtr.Zero && !seen.Add(handle))
            {
                continue;
            }

            result.Add(item);
        }

        return result;
    }

    public static UiNode BuildNode(AutomationElement element, int depth)
    {
        var children = depth <= 0
            ? []
            : element.FindAllChildren().Select(child => BuildNode(child, depth - 1)).ToArray();

        // Every property goes through ValueOrDefault — a real UIA element can refuse an
        // individual property (e.g. AutomationId) while happily answering the others, and a
        // direct getter throws in that case instead of returning null.
        var automationId = element.Properties.AutomationId.ValueOrDefault;
        var name = element.Properties.Name.ValueOrDefault;

        return new UiNode(
            element.Properties.ControlType.ValueOrDefault.ToString(),
            string.IsNullOrEmpty(automationId) ? null : automationId,
            string.IsNullOrEmpty(name) ? null : name,
            element.Properties.IsEnabled.ValueOrDefault,
            element.Properties.IsOffscreen.ValueOrDefault,
            element.Properties.BoundingRectangle.ValueOrDefault.ToString(),
            children);
    }

    private static bool TryCapturePrintWindow(IntPtr handle, out Bitmap? bitmap)
    {
        if (!GetWindowRect(handle, out var rect))
        {
            bitmap = null;
            return false;
        }

        var width = rect.Right - rect.Left;
        var height = rect.Bottom - rect.Top;
        if (width <= 0 || height <= 0)
        {
            bitmap = null;
            return false;
        }

        var candidate = new Bitmap(width, height, PixelFormat.Format32bppArgb);
        using var graphics = Graphics.FromImage(candidate);
        var hdc = graphics.GetHdc();
        bool success;
        try
        {
            success = PrintWindow(handle, hdc, PwRenderFullContent);
        }
        finally
        {
            graphics.ReleaseHdc(hdc);
        }

        if (!success)
        {
            candidate.Dispose();
            bitmap = null;
            return false;
        }

        bitmap = candidate;
        return true;
    }

    [DllImport("user32.dll")]
    private static extern bool PrintWindow(IntPtr hWnd, IntPtr hdcBlt, uint nFlags);

    [DllImport("user32.dll")]
    private static extern bool GetWindowRect(IntPtr hWnd, out NativeRect lpRect);

    [StructLayout(LayoutKind.Sequential)]
    private struct NativeRect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }
}
