namespace Platee.Johann.Infrastructure.Mail;

using System.Diagnostics;
using Platee.Johann.Application.Mail;

/// <summary>
/// Last-resort way to open a draft: <c>mailto:</c> with plain text. It cannot carry attachments,
/// so each attachment is shown selected in Explorer, ready to drag into the mail.
/// </summary>
public sealed class MailtoChannel : IMailChannel
{
    /// <inheritdoc/>
    public Task ComposeAsync(MailDraft draft, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        var uri = MailtoText.BuildUri(draft.Subject, MailtoText.ToPlainText(draft.BodyMarkdown));
        Process.Start(new ProcessStartInfo(uri) { UseShellExecute = true });

        foreach (var path in draft.Attachments.Where(File.Exists))
        {
            Process.Start(new ProcessStartInfo("explorer.exe", $"/select,\"{path}\"") { UseShellExecute = true });
        }

        return Task.CompletedTask;
    }
}
