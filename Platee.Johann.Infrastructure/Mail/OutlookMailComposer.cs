namespace Platee.Johann.Infrastructure.Mail;

using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Mail;

/// <summary>One concrete way of opening a draft.</summary>
public interface IMailChannel
{
    /// <summary>Opens the draft; throws when this way is not available right now.</summary>
    /// <param name="draft">The mail.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>A task completing once the draft is open.</returns>
    Task ComposeAsync(MailDraft draft, CancellationToken ct);
}

/// <summary>
/// Opens drafts in classic Outlook with body, signature and attachments, and falls back to
/// <c>mailto:</c> when classic Outlook is not in use or does not respond (#57).
/// </summary>
public sealed class OutlookMailComposer : IMailComposer
{
    private readonly OutlookEnvironment environment;
    private readonly IMailChannel classic;
    private readonly IMailChannel mailto;
    private readonly Action<string>? logWarning;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="environment">Which Outlook is in use.</param>
    /// <param name="classic">Classic Outlook via COM.</param>
    /// <param name="mailto">The <c>mailto:</c> fallback.</param>
    /// <param name="logWarning">Receives the reason when classic Outlook failed.</param>
    public OutlookMailComposer(
        OutlookEnvironment environment, IMailChannel classic, IMailChannel mailto, Action<string>? logWarning = null)
    {
        this.environment = environment;
        this.classic = classic;
        this.mailto = mailto;
        this.logWarning = logWarning;
    }

    /// <inheritdoc/>
    public async Task<MailComposeResult> ComposeAsync(MailDraft draft, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (this.environment.CanUseClassicOutlook)
        {
            try
            {
                await this.classic.ComposeAsync(draft, ct);
                return new MailComposeResult(MailChannel.Outlook, AttachmentsIncluded: true);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                // The user still gets a mail; the reason goes to the log for support.
                this.logWarning?.Invoke($"Outlook-Entwurf fehlgeschlagen, mailto-Rückfall: {ex.GetType().Name}: {ex.Message}");
            }
        }

        await this.mailto.ComposeAsync(draft, ct);
        return new MailComposeResult(MailChannel.Mailto, AttachmentsIncluded: draft.Attachments.Count == 0);
    }
}
