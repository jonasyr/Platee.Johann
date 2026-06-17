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
                    font-family: 'Segoe UI', sans-serif;
                    font-size: 14px; color: #333; line-height: 1.65;
                    margin: 28px 32px;
                }
                h1 {
                    color: #E63123; font-size: 22px; font-weight: 600;
                    margin-bottom: 16px;
                }
                h2 {
                    font-size: 15px; font-weight: 600; color: #1a1a1a;
                    margin-top: 20px; margin-bottom: 10px;
                    padding-bottom: 5px;
                    border-bottom: 1px solid #E63123;
                }
                h3, strong { font-size: 14px; font-weight: 600; color: #222; }
                p { margin: 4px 0 10px 0; }
                ul {
                    list-style: none; padding-left: 0;
                    margin: 6px 0 12px 0;
                }
                li {
                    margin-bottom: 6px; padding-left: 16px;
                    position: relative;
                }
                li::before {
                    content: '\2022'; color: #E63123; font-weight: 700;
                    position: absolute; left: 0; font-size: 13px;
                }
                hr {
                    border: none; border-top: 1px solid #e0e0e0;
                    margin: 18px 0;
                }
                code {
                    background: #f5f5f5; padding: 1px 5px;
                    border-radius: 3px; font-size: 13px;
                }
            </style>
            </head>
            <body>{{body}}</body>
            </html>
            """;
    }
}
