namespace Platee.Johann.Infrastructure.Mail;

using Platee.Johann.Domain.Services;

/// <summary>
/// Plain text and URI for the <c>mailto:</c> fallback, which knows neither HTML nor attachments.
/// </summary>
public static class MailtoText
{
    /// <summary>
    /// Longest URI handed to the shell. Outlook and the Windows URL handler cut or reject
    /// <c>mailto:</c> links beyond roughly 2000 characters.
    /// </summary>
    public const int MaxUriLength = 2000;

    private const string ShortenedNote = "\n\n[… gekürzt – vollständiger Text in Johann]";

    /// <summary>Drops headings, bold and italic markers; list dashes stay readable as text.</summary>
    /// <param name="markdown">Body markdown.</param>
    /// <returns>Plain text.</returns>
    public static string ToPlainText(string markdown) => InlineMarkdown.ToPlainText(markdown);

    /// <summary>Builds a <c>mailto:</c> URI, shortening the body to stay below <see cref="MaxUriLength"/>.</summary>
    /// <param name="subject">Subject.</param>
    /// <param name="plainBody">Plain-text body.</param>
    /// <returns>The URI.</returns>
    public static string BuildUri(string subject, string plainBody)
    {
        var prefix = $"mailto:?subject={Uri.EscapeDataString(subject)}&body=";
        var full = prefix + Uri.EscapeDataString(plainBody);
        if (full.Length <= MaxUriLength)
        {
            return full;
        }

        // Escaping grows umlauts to six characters, so shrink the text until the escaped form fits.
        var budget = MaxUriLength - prefix.Length - Uri.EscapeDataString(ShortenedNote).Length;
        var length = plainBody.Length;
        string escaped;
        do
        {
            length = Math.Min(length - 1, (int)(length * 0.9));
            escaped = Uri.EscapeDataString(plainBody[..Math.Max(0, length)].TrimEnd());
        }
        while (escaped.Length > budget && length > 0);

        return prefix + escaped + Uri.EscapeDataString(ShortenedNote);
    }

}
