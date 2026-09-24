namespace Platee.Johann.UiDriver.Automation;

using System.Drawing.Imaging;
using FlaUI.Core.AutomationElements;

/// <summary>
/// Small pure/IO helpers split out of <see cref="JohannSession"/> to keep it focused: the
/// main-window title rule, screenshot capture, and UI-tree traversal.
/// </summary>
internal static class JohannWindows
{
    private const string MainWindowTitlePrefix = "Platé.Johann ";
    private const string SubWindowSeparator = " – ";

    /// <summary>
    /// The main window's title is "Platé.Johann {version}" (see MainWindow.xaml). Every other
    /// Johann window/dialog is titled "Platé.Johann – …", so a title that starts with the plain
    /// prefix but does not contain the en-dash separator identifies the main window.
    /// </summary>
    public static bool IsMainWindow(string? title) =>
        title is not null
        && title.StartsWith(MainWindowTitlePrefix, StringComparison.Ordinal)
        && !title.Contains(SubWindowSeparator, StringComparison.Ordinal);

    public static string? TrySaveScreenshot(AutomationElement element, string prefix)
    {
        try
        {
            return SaveScreenshot(element, prefix);
        }
        catch (Exception)
        {
            // A screenshot is diagnostic best-effort; losing it must not hide the real failure.
            return null;
        }
    }

    public static string SaveScreenshot(AutomationElement element, string prefix)
    {
        var directory = Path.Combine(Path.GetTempPath(), "johann-ui-driver");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, $"{prefix}-{DateTime.UtcNow:yyyyMMdd-HHmmssfff}.png");
        element.Capture().Save(path, ImageFormat.Png);
        return path;
    }

    public static UiNode BuildNode(AutomationElement element, int depth)
    {
        var children = depth <= 0
            ? []
            : element.FindAllChildren().Select(child => BuildNode(child, depth - 1)).ToArray();

        return new UiNode(
            element.ControlType.ToString(),
            string.IsNullOrEmpty(element.AutomationId) ? null : element.AutomationId,
            string.IsNullOrEmpty(element.Name) ? null : element.Name,
            element.IsEnabled,
            element.IsOffscreen,
            element.BoundingRectangle.ToString(),
            children);
    }
}
