namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Mail;
using Platee.Johann.Infrastructure.Mail;

/// <summary>
/// Mail-Knöpfe aus #57: HTML für Outlook, Klartext für den mailto-Rückfall und die Wahl des Wegs.
/// </summary>
public sealed class MailComposerTests
{
    private static readonly MailDraft Draft = new("Betreff", "Text", [@"C:\x\eintrag.pdf"]);

    // ── HTML für Outlook ─────────────────────────────────────────────────────
    [Fact]
    public void Markdown_bold_becomes_strong_instead_of_asterisks()
    {
        // Sicherung der Abhängigkeit aus #73: Erst wenn diese Wandlung greift, darf die
        // E-Mail Markdown liefern.
        var html = MailHtml.ToFragment("Das ist **wichtig**.");

        html.Should().Contain("<strong>wichtig</strong>").And.NotContain("**");
    }

    [Fact]
    public void Raw_html_from_the_model_is_not_passed_through()
        => MailHtml.ToFragment("<script>alert(1)</script>").Should().NotContain("<script>");

    [Fact]
    public void The_fragment_carries_an_explicit_font()
        // Ohne Schriftangabe setzt Outlooks Word-Editor eingefügtes HTML in Times New Roman.
        => MailHtml.ToFragment("Text").Should().Contain("font-family");

    [Fact]
    public void Content_goes_right_after_the_body_tag_so_the_signature_stays_below()
    {
        const string outlook = "<html><head></head><body lang=DE style='x'><p>Signatur</p></body></html>";

        var merged = MailHtml.InsertAtBodyStart(outlook, "<p>Inhalt</p>");

        merged.Should().Be("<html><head></head><body lang=DE style='x'><p>Inhalt</p><p>Signatur</p></body></html>");
    }

    [Fact]
    public void Without_a_body_tag_the_content_is_prepended()
        => MailHtml.InsertAtBodyStart("<p>Signatur</p>", "<p>Inhalt</p>").Should().Be("<p>Inhalt</p><p>Signatur</p>");

    // ── mailto-Rückfall ──────────────────────────────────────────────────────
    [Fact]
    public void Plain_text_drops_markdown_markers_but_keeps_list_dashes()
        => MailtoText.ToPlainText("### Kopf\n**fett** und *kursiv*\n- Punkt").Should().Be("Kopf\nfett und kursiv\n- Punkt");

    [Fact]
    public void The_mailto_uri_stays_below_the_length_outlook_accepts()
    {
        var uri = MailtoText.BuildUri("Betreff", new string('a', 5000));

        uri.Length.Should().BeLessThanOrEqualTo(MailtoText.MaxUriLength);
        Uri.UnescapeDataString(uri).Should().Contain("gekürzt");
    }

    [Fact]
    public void A_short_mail_is_not_marked_as_shortened()
        => Uri.UnescapeDataString(MailtoText.BuildUri("Betreff", "kurz")).Should().NotContain("gekürzt");

    // ── Wahl des Wegs ────────────────────────────────────────────────────────
    [Fact]
    public async Task Classic_outlook_is_used_when_it_is_available_and_the_new_one_is_not_active()
    {
        var (composer, classic, mailto) = Composer(classicAvailable: true, newOutlookActive: false);

        var result = await composer.ComposeAsync(Draft);

        result.Should().Be(new MailComposeResult(MailChannel.Outlook, AttachmentsIncluded: true));
        classic.Calls.Should().Be(1);
        mailto.Calls.Should().Be(0);
    }

    [Fact]
    public async Task With_the_new_outlook_active_classic_com_is_not_even_tried()
    {
        // COM würde sonst womöglich das klassische Outlook im Hintergrund starten.
        var (composer, classic, mailto) = Composer(classicAvailable: true, newOutlookActive: true);

        var result = await composer.ComposeAsync(Draft);

        result.Channel.Should().Be(MailChannel.Mailto);
        classic.Calls.Should().Be(0);
        mailto.Calls.Should().Be(1);
    }

    [Fact]
    public async Task A_com_failure_falls_back_to_mailto_and_is_logged()
    {
        var logged = new List<string>();
        var (composer, _, mailto) = Composer(classicAvailable: true, newOutlookActive: false, classicThrows: true, log: logged.Add);

        var result = await composer.ComposeAsync(Draft);

        result.Should().Be(new MailComposeResult(MailChannel.Mailto, AttachmentsIncluded: false));
        mailto.Calls.Should().Be(1);
        logged.Should().ContainSingle().Which.Should().Contain("Outlook");
    }

    [Fact]
    public async Task Cancellation_is_not_turned_into_a_fallback()
    {
        var (composer, _, mailto) = Composer(classicAvailable: true, newOutlookActive: false);
        using var cts = new CancellationTokenSource();
        await cts.CancelAsync();

        var act = () => composer.ComposeAsync(Draft, cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        mailto.Calls.Should().Be(0);
    }


    // ── Neues Outlook (.eml-Entwurf) ─────────────────────────────────────────
    [Fact]
    public async Task With_the_new_outlook_active_and_installed_the_eml_draft_is_used()
    {
        var (composer, classic, newOutlook, mailto) = ComposerWithNewOutlook(
            classicAvailable: true, newOutlookActive: true, newOutlookInstalled: true);

        var result = await composer.ComposeAsync(Draft);

        result.Should().Be(new MailComposeResult(MailChannel.Outlook, AttachmentsIncluded: true));
        (classic.Calls, newOutlook.Calls, mailto.Calls).Should().Be((0, 1, 0));
    }

    [Fact]
    public async Task A_machine_with_only_the_new_outlook_uses_it_even_without_the_registry_switch()
    {
        // Ohne klassisches Office setzt niemand UseNewOutlook – das neue Outlook ist dann das einzige.
        var (composer, _, newOutlook, _) = ComposerWithNewOutlook(
            classicAvailable: false, newOutlookActive: false, newOutlookInstalled: true);

        await composer.ComposeAsync(Draft);

        newOutlook.Calls.Should().Be(1);
    }

    [Fact]
    public async Task Classic_users_with_the_new_outlook_merely_installed_stay_on_classic()
    {
        // Wie auf diesem Rechner: beide installiert, klassisch in Gebrauch.
        var (composer, classic, newOutlook, _) = ComposerWithNewOutlook(
            classicAvailable: true, newOutlookActive: false, newOutlookInstalled: true);

        await composer.ComposeAsync(Draft);

        (classic.Calls, newOutlook.Calls).Should().Be((1, 0));
    }

    [Fact]
    public async Task A_failing_eml_draft_falls_back_to_mailto_and_is_logged()
    {
        var logged = new List<string>();
        var (composer, _, _, mailto) = ComposerWithNewOutlook(
            classicAvailable: false, newOutlookActive: true, newOutlookInstalled: true, newOutlookThrows: true, log: logged.Add);

        var result = await composer.ComposeAsync(Draft);

        result.Channel.Should().Be(MailChannel.Mailto);
        mailto.Calls.Should().Be(1);
        logged.Should().ContainSingle().Which.Should().Contain("neues Outlook");
    }

    // ── Registry ─────────────────────────────────────────────────────────────
    [Theory]
    [InlineData(1, true)]
    [InlineData(0, false)]
    [InlineData(null, false)]
    public void UseNewOutlook_decides_whether_the_new_outlook_is_active(int? value, bool expected)
        => new OutlookEnvironment(readUseNewOutlook: () => value, classicRegistered: () => true)
            .IsNewOutlookActive.Should().Be(expected);

    private static (OutlookMailComposer Composer, FakeChannel Classic, FakeChannel Mailto) Composer(
        bool classicAvailable, bool newOutlookActive, bool classicThrows = false, Action<string>? log = null)
    {
        var (composer, classic, _, mailto) = ComposerWithNewOutlook(
            classicAvailable, newOutlookActive, newOutlookInstalled: false, classicThrows: classicThrows, log: log);
        return (composer, classic, mailto);
    }

    private static (OutlookMailComposer Composer, FakeChannel Classic, FakeChannel NewOutlook, FakeChannel Mailto)
        ComposerWithNewOutlook(
            bool classicAvailable,
            bool newOutlookActive,
            bool newOutlookInstalled,
            bool classicThrows = false,
            bool newOutlookThrows = false,
            Action<string>? log = null)
    {
        var environment = new OutlookEnvironment(
            () => newOutlookActive ? 1 : 0, () => classicAvailable, () => newOutlookInstalled);
        var classic = new FakeChannel(classicThrows);
        var newOutlook = new FakeChannel(newOutlookThrows);
        var mailto = new FakeChannel(throws: false);
        return (new OutlookMailComposer(environment, classic, newOutlook, mailto, log), classic, newOutlook, mailto);
    }

    private sealed class FakeChannel(bool throws) : IMailChannel
    {
        public int Calls { get; private set; }

        public Task ComposeAsync(MailDraft draft, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            this.Calls++;
            return throws
                ? Task.FromException(new System.Runtime.InteropServices.COMException("Outlook antwortet nicht"))
                : Task.CompletedTask;
        }
    }
}
