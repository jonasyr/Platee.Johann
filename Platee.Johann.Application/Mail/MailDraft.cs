namespace Platee.Johann.Application.Mail;

/// <summary>
/// A mail ready to be opened in the user's mail client, never sent automatically.
/// </summary>
/// <param name="Subject">Subject line.</param>
/// <param name="BodyMarkdown">Body as markdown; the composer converts it for the client.</param>
/// <param name="Attachments">
/// Full paths of files to attach. A parameter rather than a rule inside the composer, so that
/// changing which button attaches the PDF stays a one-line decision (#57).
/// </param>
public sealed record MailDraft(string Subject, string BodyMarkdown, IReadOnlyList<string> Attachments);
