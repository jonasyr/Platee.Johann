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
                    padding: 7px 14px; margin: 0 -16px 14px -16px;
                    border-radius: 4px;
                }
                h3, strong { font-size: 14px; font-weight: 600; color: #222; }
                p { margin: 5px 0 10px 0; color: #444; }
                /* Version sections as cards */
                hr { display: none; }
                h2 ~ h2 { margin-top: 0; }
                /* Wrap each version block visually */
                body > h2 { margin-top: 18px; }
                body > h2:first-of-type { margin-top: 0; }
                /* Card-like sections via h2 siblings */
                body > p, body > ul, body > h3, body > strong {
                    padding: 0 14px;
                }
                ul {
                    list-style: none; padding-left: 14px;
                    margin: 6px 0 12px 0;
                }
                li {
                    margin-bottom: 5px; padding-left: 14px;
                    position: relative;
                }
                li::before {
                    content: '\2022'; color: #E63123; font-weight: 700;
                    position: absolute; left: 0;
                }
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
