namespace Platee.Johann.Application.Interfaces;

using Platee.Johann.Application.Mail;

/// <summary>How a draft reached the user, so the UI can explain what to do next.</summary>
public enum MailChannel
{
    /// <summary>Opened in Outlook with body, signature and attachments.</summary>
    Outlook,

    /// <summary>
    /// Opened through <c>mailto:</c>: plain text, no attachment. The caller shows the attachment
    /// in Explorer so the user can drag it into the mail.
    /// </summary>
    Mailto,
}

/// <summary>Result of opening a draft.</summary>
/// <param name="Channel">The way the draft was opened.</param>
/// <param name="AttachmentsIncluded">Whether the draft's attachments are part of the mail.</param>
public sealed record MailComposeResult(MailChannel Channel, bool AttachmentsIncluded);

/// <summary>Opens a mail draft in the user's mail client for review; never sends it.</summary>
public interface IMailComposer
{
    /// <summary>Opens <paramref name="draft"/> for the user to review and send.</summary>
    /// <param name="draft">The mail to open.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>How the draft was opened.</returns>
    Task<MailComposeResult> ComposeAsync(MailDraft draft, CancellationToken ct = default);
}
