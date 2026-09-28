namespace Platee.Johann.UiDriver.Automation;

/// <summary>
/// Thrown by <see cref="JohannSession.Launch"/> when a top-level window other than the main
/// window appears while waiting for startup (e.g. an unexpected error dialog). A screenshot of
/// the offending window is taken before throwing, when possible.
/// </summary>
public sealed class UnexpectedWindowException : Exception
{
    public UnexpectedWindowException(string title, string? screenshotPath)
        : base(BuildMessage(title, screenshotPath))
    {
        this.Title = title;
        this.ScreenshotPath = screenshotPath;
    }

    public string Title { get; }

    public string? ScreenshotPath { get; }

    private static string BuildMessage(string title, string? screenshotPath) =>
        screenshotPath is null
            ? $"Unerwartetes Fenster '{title}'."
            : $"Unerwartetes Fenster '{title}' (Screenshot: {screenshotPath}).";
}
