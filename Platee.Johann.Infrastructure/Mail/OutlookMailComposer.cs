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
/// Opens a draft in whichever Outlook the user works with, each time with body, signature and
/// attachments (#57):
/// <list type="number">
/// <item>classic Outlook through COM,</item>
/// <item>new Outlook through an .eml draft — it has no COM,</item>
/// <item><c>mailto:</c> as the last resort, without attachments.</item>
/// </list>
/// A failing way falls through to <c>mailto:</c>; the reason goes to the log for support.
/// </summary>
public sealed class OutlookMailComposer : IMailComposer
{
    private readonly OutlookEnvironment environment;
    private readonly IMailChannel classic;
    private readonly IMailChannel newOutlook;
    private readonly IMailChannel mailto;
    private readonly Action<string>? logWarning;

    /// <summary>Initializes a new instance.</summary>
    /// <param name="environment">Which Outlook is in use.</param>
    /// <param name="classic">Classic Outlook via COM.</param>
    /// <param name="newOutlook">New Outlook via an .eml draft.</param>
    /// <param name="mailto">The <c>mailto:</c> fallback.</param>
    /// <param name="logWarning">Receives the reason when an Outlook way failed.</param>
    public OutlookMailComposer(
        OutlookEnvironment environment,
        IMailChannel classic,
        IMailChannel newOutlook,
        IMailChannel mailto,
        Action<string>? logWarning = null)
    {
        this.environment = environment;
        this.classic = classic;
        this.newOutlook = newOutlook;
        this.mailto = mailto;
        this.logWarning = logWarning;
    }

    /// <inheritdoc/>
    public async Task<MailComposeResult> ComposeAsync(MailDraft draft, CancellationToken ct = default)
    {
        ct.ThrowIfCancellationRequested();
        if (this.environment.CanUseClassicOutlook
            && await this.TryAsync(this.classic, draft, "klassisches Outlook", ct))
        {
            return new MailComposeResult(MailChannel.Outlook, AttachmentsIncluded: true);
        }

        if (this.environment.CanUseNewOutlook
            && await this.TryAsync(this.newOutlook, draft, "neues Outlook", ct))
        {
            return new MailComposeResult(MailChannel.Outlook, AttachmentsIncluded: true);
        }

        await this.mailto.ComposeAsync(draft, ct);
        return new MailComposeResult(MailChannel.Mailto, AttachmentsIncluded: draft.Attachments.Count == 0);
    }

    private async Task<bool> TryAsync(IMailChannel channel, MailDraft draft, string label, CancellationToken ct)
    {
        try
        {
            await channel.ComposeAsync(draft, ct);
            return true;
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            this.logWarning?.Invoke($"{label}: Entwurf fehlgeschlagen, mailto-Rückfall: {ex.GetType().Name}: {ex.Message}");
            return false;
        }
    }
}
