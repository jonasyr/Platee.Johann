namespace Platee.Johann.Infrastructure.Mail;

using System.Text.RegularExpressions;
using Platee.Johann.Infrastructure.Renderers;

/// <summary>
/// Turns a draft's markdown into the HTML that goes into an Outlook mail body.
/// </summary>
public static partial class MailHtml
{
    /// <summary>
    /// Wraps the converted markdown in an explicit font. Outlook composes with Word, which renders
    /// inserted HTML without a font in Times New Roman instead of the user's default.
    /// </summary>
    /// <param name="markdown">Body markdown; raw HTML in it is escaped, not passed through.</param>
    /// <returns>An HTML fragment.</returns>
    public static string ToFragment(string markdown)
        => "<div style=\"font-family: Aptos, Calibri, Arial, sans-serif; font-size: 11pt;\">"
           + MarkdownHelper.ToHtml(markdown)
           + "</div>";

    /// <summary>
    /// Inserts <paramref name="fragment"/> right after the opening body tag of Outlook's HTML, so
    /// that the signature Outlook added on <c>Display()</c> stays below the content.
    /// </summary>
    /// <param name="outlookHtml">The body Outlook produced, including the signature.</param>
    /// <param name="fragment">The content to insert.</param>
    /// <returns>The merged HTML.</returns>
    public static string InsertAtBodyStart(string? outlookHtml, string fragment)
    {
        var html = outlookHtml ?? string.Empty;
        var body = BodyTag().Match(html);
        return body.Success
            ? html.Insert(body.Index + body.Length, fragment)
            : fragment + html;
    }

    [GeneratedRegex("<body\\b[^>]*>", RegexOptions.IgnoreCase)]
    private static partial Regex BodyTag();
}
