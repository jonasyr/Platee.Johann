namespace Platee.Johann.Tests.Unit;

using System.Text;
using System.Text.RegularExpressions;
using FluentAssertions;
using Platee.Johann.Infrastructure.Mail;

/// <summary>
/// Der .eml-Entwurf für das neue Outlook (#57): Es kann kein COM, öffnet aber eine Datei mit
/// <c>X-Unsent: 1</c> als bearbeitbaren Entwurf – mit Anhang und eigener Signatur.
/// </summary>
public sealed class EmlDraftTests
{
    private static readonly Guid Id = Guid.Parse("11111111-2222-3333-4444-555555555555");
    private static readonly DateTimeOffset Now = new(2026, 9, 21, 15, 39, 0, TimeSpan.FromHours(2));

    [Fact]
    public void The_draft_is_marked_unsent_and_carries_its_own_message_id()
    {
        // Ohne Message-ID speichert das neue Outlook den Entwurf nicht (Fix 1.2026.105).
        var eml = Build();

        Header(eml, "X-Unsent").Should().Be("1");
        Header(eml, "Message-ID").Should().Be("<11111111222233334444555555555555@johann.local>");
        Header(eml, "Date").Should().Be("Mon, 21 Sep 2026 15:39:00 +0200");
    }

    [Fact]
    public void A_draft_has_no_recipient_and_no_sender()
    {
        // Der Nutzer wählt den Empfänger; Outlook setzt den Absender aus dem Konto.
        var eml = Build();

        HeaderBlock(eml).Should().NotMatchRegex("(?im)^(To|From|Cc):");
    }

    [Fact]
    public void The_subject_survives_umlauts_and_dashes()
    {
        var eml = Build(subject: "Aufgaben – Übergabe Größe");

        var decoded = string.Concat(Regex.Matches(HeaderBlock(eml), @"=\?utf-8\?B\?([^?]+)\?=")
            .Select(m => Encoding.UTF8.GetString(Convert.FromBase64String(m.Groups[1].Value))));
        decoded.Should().Be("Aufgaben – Übergabe Größe");
    }

    [Fact]
    public void A_long_subject_is_split_into_encoded_words_below_the_line_limit()
    {
        var eml = Build(subject: new string('Ä', 120));

        HeaderBlock(eml).Split("\r\n").Should().OnlyContain(line => line.Length <= 78);
    }

    [Fact]
    public void The_html_body_decodes_to_a_complete_utf8_document()
    {
        var eml = Build(html: "<p>Größe <strong>fett</strong></p>");

        var html = DecodedPart(eml, "text/html");
        html.Should().Contain("<meta charset=\"utf-8\">").And.Contain("<body><p>Größe <strong>fett</strong></p></body>");
    }

    [Fact]
    public void Each_attachment_is_a_named_base64_part()
    {
        var eml = Build(attachments: [("Übergabe.pdf", "%PDF-1.4"u8.ToArray())]);

        eml.Should().Contain("Content-Type: application/pdf");
        eml.Should().Contain("Content-Disposition: attachment");
        DecodedPart(eml, "application/pdf").Should().Be("%PDF-1.4");
        eml.Should().Contain("filename*=utf-8''%C3%9Cbergabe.pdf");
    }

    [Fact]
    public void Lines_end_in_crlf_as_mime_requires()
    {
        var eml = Build();

        eml.Replace("\r\n", string.Empty, StringComparison.Ordinal).Should().NotContain("\n");
        eml.Should().EndWith("--\r\n");
    }

    private static string Build(
        string subject = "Betreff", string html = "<p>Text</p>", (string Name, byte[] Content)[]? attachments = null)
        => EmlDraft.Build(subject, html, attachments ?? [], Now, Id);

    private static string HeaderBlock(string eml) => eml[..eml.IndexOf("\r\n\r\n", StringComparison.Ordinal)];

    private static string? Header(string eml, string name)
        => Regex.Match(HeaderBlock(eml), $"(?im)^{Regex.Escape(name)}: (.*)$").Groups[1].Value.TrimEnd('\r');

    private static string DecodedPart(string eml, string contentType)
    {
        var start = eml.IndexOf($"Content-Type: {contentType}", StringComparison.Ordinal);
        var body = eml.IndexOf("\r\n\r\n", start, StringComparison.Ordinal) + 4;
        var end = eml.IndexOf("\r\n--", body, StringComparison.Ordinal);
        return Encoding.UTF8.GetString(Convert.FromBase64String(eml[body..end].Replace("\r\n", string.Empty)));
    }
}
