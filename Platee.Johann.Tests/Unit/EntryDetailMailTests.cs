using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Mail;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.UI.ViewModels;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// Die Mail-Knöpfe der Detailleiste (#57): „Aufgaben" öffnet die interne Mail mit PDF,
/// „E-Mail" die externe ohne Anhang. Fehlt der Abschnitt, wird er vorher erzeugt.
/// </summary>
public sealed class EntryDetailMailTests
{
    private readonly List<string> logged = [];
    private readonly IMailComposer composer = Substitute.For<IMailComposer>();
    private readonly IEntryProcessor processor = Substitute.For<IEntryProcessor>();
    private MailDraft? sent;

    public EntryDetailMailTests()
    {
        this.composer.ComposeAsync(Arg.Do<MailDraft>(d => this.sent = d), Arg.Any<CancellationToken>())
            .Returns(new MailComposeResult(MailChannel.Outlook, AttachmentsIncluded: true));
    }

    [Fact]
    public async Task The_task_mail_carries_the_intro_the_tasks_and_the_pdf()
    {
        var vm = this.CreateVm(PdfRenderer());
        vm.Entry = MakeEntry();

        await vm.OpenTaskMailCommand.ExecuteAsync(null);

        this.sent.Should().NotBeNull();
        this.sent!.Subject.Should().Be("Aufgaben – Johann – Übergabe");
        this.sent.BodyMarkdown.Should().StartWith("Aufgaben zu Johann:").And.Contain("- Angebot schicken");
        this.sent.Attachments.Should().ContainSingle().Which.Should().EndWith("entry.pdf");
        this.logged.Should().Contain(m => m.Contains("Outlook"));
    }

    [Fact]
    public async Task A_missing_task_section_is_generated_before_the_mail_opens()
    {
        var entry = MakeEntry() with { TaskList = null };
        this.processor.GenerateSectionAsync(Arg.Any<Entry>(), BuiltInSections.TaskList,
                Arg.Any<IProgress<ProcessingProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(entry with { TaskList = "Nachträglich.\n\n- Neue Aufgabe" });
        var vm = this.CreateVm(PdfRenderer());
        vm.Entry = entry;

        await vm.OpenTaskMailCommand.ExecuteAsync(null);

        this.sent!.BodyMarkdown.Should().Contain("- Neue Aufgabe");
    }

    [Fact]
    public async Task Without_tasks_no_mail_is_opened_and_the_user_is_told_why()
    {
        var vm = this.CreateVm(PdfRenderer(), withProcessor: false);
        vm.Entry = MakeEntry() with { TaskList = null };

        await vm.OpenTaskMailCommand.ExecuteAsync(null);

        this.sent.Should().BeNull();
        this.logged.Should().Contain(m => m.Contains("Aufgaben"));
    }


    [Fact]
    public async Task When_the_pdf_fails_no_task_mail_is_opened()
    {
        // Der Begleittext kündigt das PDF an; ohne Anhang wäre die Mail irreführend (Codex, PR #87).
        var renderer = Substitute.For<IEntryRenderer>();
        renderer.RendererName.Returns("PDF");
        renderer.RenderAsync(Arg.Any<Entry>(), Arg.Any<RenderOptions>(), Arg.Any<CancellationToken>())
            .Returns<RenderResult>(_ => throw new IOException("Ausgabeordner nicht erreichbar"));
        var vm = this.CreateVm(renderer);
        vm.Entry = MakeEntry();

        await vm.OpenTaskMailCommand.ExecuteAsync(null);

        this.sent.Should().BeNull();
        this.logged.Should().Contain(m => m.Contains("Ausgabeordner nicht erreichbar"));
        this.logged.Should().Contain(m => m.Contains("Aufgaben-Mail") && m.Contains("PDF"));
    }

    [Fact]
    public async Task The_external_mail_has_no_attachment_and_uses_the_betreff_line()
    {
        var vm = this.CreateVm(PdfRenderer());
        vm.Entry = MakeEntry() with { EmailText = "Betreff: Angebot\n\nSehr geehrter Herr Vogel,\n\nText." };

        await vm.OpenEmailCommand.ExecuteAsync(null);

        this.sent!.Subject.Should().Be("Angebot");
        this.sent.Attachments.Should().BeEmpty("die externe Mail bekommt kein PDF (#57)");
    }

    [Fact]
    public async Task A_missing_mail_text_is_generated_before_the_mail_opens()
    {
        var entry = MakeEntry() with { EmailText = null };
        this.processor.GenerateSectionAsync(Arg.Any<Entry>(), BuiltInSections.EmailText,
                Arg.Any<IProgress<ProcessingProgress>?>(), Arg.Any<CancellationToken>())
            .Returns(entry with { EmailText = "Betreff: Neu\n\nGuten Tag,\n\nText." });
        var vm = this.CreateVm(PdfRenderer());
        vm.Entry = entry;

        await vm.OpenEmailCommand.ExecuteAsync(null);

        this.sent!.Subject.Should().Be("Neu");
    }

    [Fact]
    public async Task The_mailto_fallback_explains_where_the_pdf_is()
    {
        this.composer.ComposeAsync(Arg.Any<MailDraft>(), Arg.Any<CancellationToken>())
            .Returns(new MailComposeResult(MailChannel.Mailto, AttachmentsIncluded: false));
        var vm = this.CreateVm(PdfRenderer());
        vm.Entry = MakeEntry();

        await vm.OpenTaskMailCommand.ExecuteAsync(null);

        this.logged.Should().Contain(m => m.Contains("PDF") && m.Contains("Explorer"));
    }

    private static IEntryRenderer PdfRenderer()
    {
        var renderer = Substitute.For<IEntryRenderer>();
        renderer.RendererName.Returns("PDF");
        renderer.RenderAsync(Arg.Any<Entry>(), Arg.Any<RenderOptions>(), Arg.Any<CancellationToken>())
            .Returns(new RenderResult([], "application/pdf", "entry.pdf"));
        return renderer;
    }

    private static Entry MakeEntry() => new()
    {
        JobId = "260921_001_abcdef12",
        SequenceNumber = 1,
        CreatedAt = new DateTimeOffset(new DateTime(2026, 9, 21)),
        Type = EntryType.Projekt,
        ProjectName = "Johann",
        Title = "Übergabe",
        SourceType = "audio",
        Status = ProcessingStatus.Empty,
        TaskList = "Kurzer Absatz.\n\n- Angebot schicken",
    };

    private EntryDetailViewModel CreateVm(IEntryRenderer renderer, bool withProcessor = true) =>
        new(
            [renderer],
            Path.GetTempPath(),
            processor: withProcessor ? this.processor : null,
            addLog: (message, _) =>
            {
                this.logged.Add(message);
                return new ProcessLogItem(message, DateTime.Now, false);
            },
            mailComposer: this.composer,
            taskMailIntro: () => "Aufgaben zu {Projekt}:");
}
