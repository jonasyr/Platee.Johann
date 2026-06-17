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
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body {
                    font-family: 'Segoe UI', 'Helvetica Neue', sans-serif;
                    font-size: 14px; color: #2c2c2c; line-height: 1.7;
                    margin: 32px 36px; -webkit-font-smoothing: antialiased;
                }
                h1 {
                    color: #E63123; font-size: 24px; font-weight: 600;
                    margin-bottom: 20px; letter-spacing: -0.3px;
                }
                h2 {
                    color: #1a1a1a; font-size: 16px; font-weight: 600;
                    margin-top: 28px; margin-bottom: 16px;
                    padding-bottom: 8px; border-bottom: 2px solid #E63123;
                    letter-spacing: -0.2px;
                }
                h3, strong {
                    color: #333; font-size: 14px; font-weight: 600;
                }
                p {
                    margin: 8px 0 12px 0; color: #444;
                }
                ul {
                    padding-left: 0; margin: 8px 0 16px 0;
                    list-style: none;
                }
                li {
                    margin-bottom: 8px; padding-left: 20px;
                    position: relative; color: #444;
                }
                li::before {
                    content: '\2013'; color: #E63123; font-weight: 700;
                    position: absolute; left: 0;
                }
                hr {
                    border: none; border-top: 1px solid #e8e8e8;
                    margin: 28px 0;
                }
                code {
                    background: #f5f5f5; padding: 2px 6px;
                    border-radius: 3px; font-size: 13px;
                    font-family: 'Cascadia Code', 'Consolas', monospace;
                }
            </style>
            </head>
            <body>{{body}}</body>
            </html>
            """;
    }
}
