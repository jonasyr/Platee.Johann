namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Mail;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;

/// <summary>
/// Die zwei Mail-Knöpfe aus #57: „Aufgaben" ist die interne Mail mit PDF, „E-Mail" die externe,
/// förmliche ohne Anhang.
/// </summary>
public sealed class MailDraftBuilderTests
{
    private const string Pdf = @"C:\Johann\output\2026-09-21\260921_001_Johann.pdf";

    [Fact]
    public void The_task_mail_is_intro_then_task_section_with_the_pdf_attached()
    {
        var draft = MailDraftBuilder.ForTasks(Entry(), "Hallo zusammen, anbei die Aufgaben zu {Projekt}.", Pdf);

        draft.Subject.Should().Be("Aufgaben – Johann – Domainübergabe");
        draft.BodyMarkdown.Should().Be(
            "Hallo zusammen, anbei die Aufgaben zu Johann.\n\n" +
            "Kurzer Absatz.\n\n- Erste Aufgabe\n- Zweite Aufgabe");
        draft.Attachments.Should().Equal(Pdf);
    }

    [Fact]
    public void The_project_placeholder_falls_back_to_the_title_when_there_is_no_project()
    {
        var draft = MailDraftBuilder.ForTasks(Entry() with { ProjectName = "" }, "Aufgaben zu {Projekt}.", Pdf);

        draft.Subject.Should().Be("Aufgaben – Domainübergabe");
        draft.BodyMarkdown.Should().StartWith("Aufgaben zu Domainübergabe.");
    }

    [Fact]
    public void An_empty_intro_leaves_only_the_task_section()
    {
        var draft = MailDraftBuilder.ForTasks(Entry(), "   ", Pdf);

        draft.BodyMarkdown.Should().Be("Kurzer Absatz.\n\n- Erste Aufgabe\n- Zweite Aufgabe");
    }

    [Fact]
    public void Without_a_pdf_the_task_mail_has_no_attachment()
        => MailDraftBuilder.ForTasks(Entry(), "Intro", pdfPath: null).Attachments.Should().BeEmpty();

    [Fact]
    public void The_external_mail_takes_its_subject_from_the_betreff_line_and_attaches_nothing()
    {
        var entry = Entry() with
        {
            EmailText = "Betreff: Angebot Sanierung\n\nSehr geehrter Herr Vogel,\n\ndas Angebot folgt.",
        };

        var draft = MailDraftBuilder.ForExternal(entry);

        draft.Subject.Should().Be("Angebot Sanierung");
        draft.BodyMarkdown.Should().Be("Sehr geehrter Herr Vogel,\n\ndas Angebot folgt.");
        draft.Attachments.Should().BeEmpty("die externe Mail bekommt kein internes Arbeitsdokument (#57)");
    }

    [Fact]
    public void Without_a_betreff_line_the_external_subject_is_project_and_title()
    {
        var draft = MailDraftBuilder.ForExternal(Entry() with { EmailText = "Guten Tag,\n\nText." });

        draft.Subject.Should().Be("Johann: Domainübergabe");
        draft.BodyMarkdown.Should().Be("Guten Tag,\n\nText.");
    }

    [Fact]
    public void Windows_line_endings_do_not_leak_into_the_body()
    {
        var draft = MailDraftBuilder.ForExternal(Entry() with { EmailText = "Betreff: X\r\n\r\nZeile eins\r\nZeile zwei" });

        draft.BodyMarkdown.Should().Be("Zeile eins\nZeile zwei");
    }

    private static Entry Entry() => new()
    {
        JobId = "260921_001_abc",
        SequenceNumber = 1,
        CreatedAt = new DateTimeOffset(2026, 9, 21, 12, 0, 0, TimeSpan.FromHours(2)),
        Type = EntryType.Projekt,
        ProjectName = "Johann",
        Title = "Domainübergabe",
        SourceType = "audio",
        Status = new ProcessingStatus(true, true, false, false, false),
        Transcript = "Ein Transkript.",
        TaskList = "Kurzer Absatz.\n\n- Erste Aufgabe\n- Zweite Aufgabe\n",
    };
}
