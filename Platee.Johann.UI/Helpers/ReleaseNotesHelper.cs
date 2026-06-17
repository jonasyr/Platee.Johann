using System.IO;
using System.Reflection;
using Platee.Johann.Infrastructure.Renderers;

namespace Platee.Johann.UI.Helpers;

public static class ReleaseNotesHelper
{
    private const string ResourceName = "Platee.Johann.UI.Assets.RELEASE_NOTES.md";

    public static bool ShouldShow(string? lastSeenVersion, string currentVersion)
    {
        return string.IsNullOrEmpty(lastSeenVersion)
               || !string.Equals(lastSeenVersion, currentVersion, StringComparison.Ordinal);
    }

    public static string LoadMarkdown(Assembly? assembly = null)
    {
        assembly ??= Assembly.GetExecutingAssembly();
        using var stream = assembly.GetManifestResourceStream(ResourceName);
        if (stream is null)
        {
            return string.Empty;
        }

        using var reader = new StreamReader(stream);
        return reader.ReadToEnd();
    }

    public static string RenderToHtml(string markdown)
    {
        var body = MarkdownHelper.ToHtml(markdown);
        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8"/>
            <style>
                body { font-family: 'Segoe UI', sans-serif; font-size: 14px; color: #333;
                       margin: 24px; line-height: 1.6; }
                h1 { color: #E63123; font-size: 22px; margin-bottom: 4px; }
                h2 { color: #444; font-size: 17px; border-bottom: 1px solid #ddd;
                      padding-bottom: 4px; margin-top: 24px; }
                h3 { color: #555; font-size: 14px; margin-top: 16px; }
                ul { padding-left: 20px; }
                li { margin-bottom: 4px; }
                hr { border: none; border-top: 1px solid #eee; margin: 20px 0; }
                code { background: #f4f4f4; padding: 2px 5px; border-radius: 3px; font-size: 13px; }
            </style>
            </head>
            <body>{{body}}</body>
            </html>
            """;
    }
}
