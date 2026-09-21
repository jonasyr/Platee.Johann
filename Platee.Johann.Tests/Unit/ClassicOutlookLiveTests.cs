namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Mail;
using Platee.Johann.Infrastructure.Mail;

/// <summary>
/// Öffnet einen echten Entwurf im klassischen Outlook (#57). Läuft nur mit
/// <c>JOHANN_OUTLOOK_LIVE=1</c>, weil er ein Fenster auf dem Bildschirm öffnet; der Entwurf wird
/// nie gesendet. Prüfen von Hand: Text über der Signatur, fett statt Sternchen, PDF angehängt.
/// </summary>
public sealed class ClassicOutlookLiveTests
{
    [SkippableFact]
    public async Task Opens_a_draft_with_signature_and_attachment()
    {
        Skip.IfNot(Environment.GetEnvironmentVariable("JOHANN_OUTLOOK_LIVE") == "1",
            "Setze JOHANN_OUTLOOK_LIVE=1, um einen echten Outlook-Entwurf zu öffnen.");
        Skip.IfNot(new OutlookEnvironment().CanUseClassicOutlook, "Klassisches Outlook ist hier nicht aktiv.");

        var pdf = Path.Combine(Path.GetTempPath(), "Johann-Livetest-57.pdf");
        await File.WriteAllBytesAsync(pdf, "%PDF-1.4\n%%EOF\n"u8.ToArray());
        var draft = new MailDraft(
            "Johann-Livetest #57 – bitte nicht senden",
            "Hallo zusammen,\n\nanbei die Aufgaben zu **Johann**.\n\n- Erste Aufgabe\n  - Unterpunkt\n- Zweite Aufgabe",
            [pdf]);

        var act = () => new ClassicOutlookChannel().ComposeAsync(draft, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }


    [SkippableFact]
    public async Task Opens_an_eml_draft_in_the_new_outlook()
    {
        Skip.IfNot(Environment.GetEnvironmentVariable("JOHANN_OUTLOOK_LIVE") == "1",
            "Setze JOHANN_OUTLOOK_LIVE=1, um einen echten Entwurf im neuen Outlook zu öffnen.");
        Skip.IfNot(File.Exists(NewOutlookChannel.DefaultLauncherPath), "Neues Outlook ist hier nicht installiert.");

        var pdf = Path.Combine(Path.GetTempPath(), "Johann-Livetest-57.pdf");
        await File.WriteAllBytesAsync(pdf, "%PDF-1.4\n%%EOF\n"u8.ToArray());
        var draft = new MailDraft(
            "Johann-Livetest #57 (neues Outlook) – bitte nicht senden",
            "Hallo zusammen,\n\nanbei die Aufgaben zu **Johann**.\n\n- Erste Aufgabe\n  - Unterpunkt\n- Zweite Aufgabe",
            [pdf]);

        var act = () => new NewOutlookChannel().ComposeAsync(draft, CancellationToken.None);

        await act.Should().NotThrowAsync();
    }
}
