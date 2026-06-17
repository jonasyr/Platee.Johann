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

        // Wrap <li> content in a span so the bullet (from list-style) stays red
        // while the text is dark. The IE/Trident engine in WPF WebBrowser
        // does not support ::before pseudo-elements.
        body = System.Text.RegularExpressions.Regex.Replace(
            body,
            @"<li>(.*?)</li>",
            """<li><span style="color:#444">$1</span></li>""",
            System.Text.RegularExpressions.RegexOptions.Singleline);

        return $$"""
            <!DOCTYPE html>
            <html>
            <head>
            <meta charset="utf-8"/>
            <style>
                * { margin: 0; padding: 0; box-sizing: border-box; }
                body {
                    font-family: 'Segoe UI', sans-serif;
                    font-size: 14px; color: #333; line-height: 1.6;
                    margin: 24px 28px;
                    background: #fafafa;
                }
                h1 {
                    color: #E63123; font-size: 22px; font-weight: 600;
                    margin-bottom: 18px;
                }
                h2 {
                    font-size: 15px; font-weight: 700; color: #fff;
                    background: #E63123;
                    padding: 7px 14px; margin: 18px 0 14px 0;
                    border-radius: 4px;
                }
                body > h2:first-of-type { margin-top: 0; }
                h3, strong { font-size: 14px; font-weight: 600; color: #222; }
                p { margin: 4px 0 8px 0; color: #444; }
                hr { display: none; }
                ul {
                    margin: 6px 0 14px 22px;
                    padding: 0;
                }
                li {
                    margin-bottom: 8px;
                    color: #E63123;
                    list-style-type: disc;
                }
                li p { margin: 0; }
                code {
                    background: #eee; padding: 1px 5px;
                    border-radius: 3px; font-size: 13px;
                }
            </style>
            </head>
            <body>{{body}}</body>
            </html>
            """;
    }
}