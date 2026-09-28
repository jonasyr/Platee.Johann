namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Platee.Johann.Application.Diagnostics;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.Parsing;
using Platee.Johann.Domain.ValueObjects;

/// <summary>
/// Einträge löschen (#55) im Prozessor. Jede Generierung speichert am Ende den ganzen
/// Eintrag; liefe sie beim Löschen noch, wäre der Eintrag danach still wieder da — samt
/// Tagesübersicht. Deshalb wird Löschen während einer Generierung abgelehnt und jede
/// Generierung für einen gelöschten Eintrag.
/// </summary>
public sealed class EntryDeletionServiceTests
{
    private static readonly DateOnly Day = new(2026, 9, 22);

    private readonly IEntryRepository repo = Substitute.For<IEntryRepository>();
    private readonly IHtmlOverviewService overview = Substitute.For<IHtmlOverviewService>();
    private readonly IEntryProcessingLogger logger = Substitute.For<IEntryProcessingLogger>();
    private readonly ILlmProvider llm = Substitute.For<ILlmProvider>();

    public EntryDeletionServiceTests()
    {
        this.llm.IsAvailable.Returns(true);
        this.llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .Returns("TEXT");
        this.repo.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(call => new EntryDeletionResult(true, @"C:\out\_Papierkorb\x", [call.Arg<string>()]));
    }

    [Fact]
    public async Task Delete_moves_the_entry_and_refreshes_the_days_overview()
    {
        var service = this.CreateService();
        var entry = MakeEntry();

        var result = await service.DeleteAsync(entry);

        result.Found.Should().BeTrue();
        await this.repo.Received(1).DeleteAsync(entry.JobId, Arg.Any<CancellationToken>());
        await this.overview.Received(1).RegenerateAsync(Day, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_of_an_entry_already_gone_still_refreshes_the_overview()
    {
        this.repo.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(EntryDeletionResult.NotFound);
        var service = this.CreateService();

        var result = await service.DeleteAsync(MakeEntry());

        result.Found.Should().BeFalse();
        await this.overview.Received(1).RegenerateAsync(Day, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_while_a_section_is_generating_is_refused()
    {
        var gate = this.HoldTheLlm();
        var service = this.CreateService();
        var entry = MakeEntry();
        var generation = service.GenerateSectionAsync(entry, BuiltInSections.TaskList);

        var act = () => service.DeleteAsync(entry);

        await act.Should().ThrowAsync<EntryBusyException>();
        await this.repo.DidNotReceive().DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        gate.SetResult("TEXT");
        await generation;
    }

    [Fact]
    public async Task Delete_while_reprocessing_is_refused()
    {
        var gate = this.HoldTheLlm();
        var service = this.CreateService();
        var entry = MakeEntry();
        var reprocess = service.ReprocessAsync(entry);

        var act = () => service.DeleteAsync(entry);

        await act.Should().ThrowAsync<EntryBusyException>();
        gate.SetResult("TEXT");
        await reprocess;
    }

    [Fact]
    public async Task Delete_while_regenerating_from_the_transcript_is_refused()
    {
        var gate = this.HoldTheLlm();
        var service = this.CreateService();
        var entry = MakeEntry();
        var regenerate = service.RegenerateFromTranscriptAsync(entry, "Korrigiert.");

        var act = () => service.DeleteAsync(entry);

        await act.Should().ThrowAsync<EntryBusyException>();
        gate.SetResult("TEXT");
        await regenerate;
    }

    [Fact]
    public async Task Delete_of_another_entry_while_one_is_busy_is_allowed()
    {
        var gate = this.HoldTheLlm();
        var service = this.CreateService();
        var busy = service.GenerateSectionAsync(MakeEntry(), BuiltInSections.TaskList);

        var result = await service.DeleteAsync(MakeEntry(jobId: "260922_002_bbbbbbbb", seq: 2));

        result.Found.Should().BeTrue();
        gate.SetResult("TEXT");
        await busy;
    }

    [Fact]
    public async Task Delete_is_allowed_again_once_the_generation_has_finished()
    {
        var service = this.CreateService();
        var entry = MakeEntry();
        await service.GenerateSectionAsync(entry, BuiltInSections.TaskList);

        var result = await service.DeleteAsync(entry);

        result.Found.Should().BeTrue();
    }

    [Fact]
    public async Task Delete_is_allowed_again_after_a_generation_failed()
    {
        this.llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new TimeoutException("offline"));
        var service = this.CreateService();
        var entry = MakeEntry();
        var failing = () => service.GenerateSectionAsync(entry, BuiltInSections.TaskList);
        await failing.Should().ThrowAsync<TimeoutException>();

        var result = await service.DeleteAsync(entry);

        result.Found.Should().BeTrue("a failed generation must not leave the entry locked forever");
    }

    [Fact]
    public async Task Generations_for_a_deleted_entry_are_refused_and_never_saved()
    {
        var service = this.CreateService();
        var entry = MakeEntry();
        await service.DeleteAsync(entry);

        var section = () => service.GenerateSectionAsync(entry, BuiltInSections.TaskList);
        var reprocess = () => service.ReprocessAsync(entry);
        var regenerate = () => service.RegenerateFromTranscriptAsync(entry, "Korrigiert.");

        await section.Should().ThrowAsync<EntryDeletedException>();
        await reprocess.Should().ThrowAsync<EntryDeletedException>();
        await regenerate.Should().ThrowAsync<EntryDeletedException>();
        await this.repo.DidNotReceive().SaveAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
        await this.repo.DidNotReceive().UpdateAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
        await this.llm.DidNotReceive().GenerateAsync(
            Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetDone_saves_the_flag()
    {
        var service = this.CreateService();

        var updated = await service.SetDoneAsync(MakeEntry(), isDone: true);

        updated.IsDone.Should().BeTrue();
        await this.repo.Received(1).UpdateAsync(Arg.Is<Entry>(e => e.IsDone), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task SetDone_on_a_deleted_entry_is_refused_and_never_saved()
    {
        // Review PR #55: "erledigt" saved straight to the repository and could write the
        // status file back while the deletion was still finishing.
        var service = this.CreateService();
        var entry = MakeEntry();
        await service.DeleteAsync(entry);

        var act = () => service.SetDoneAsync(entry, isDone: true);

        await act.Should().ThrowAsync<EntryDeletedException>();
        await this.repo.DidNotReceive().SaveAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
        await this.repo.DidNotReceive().UpdateAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Delete_while_the_done_flag_is_being_saved_is_refused()
    {
        var gate = new TaskCompletionSource();
        this.repo.UpdateAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>()).Returns(gate.Task);
        var service = this.CreateService();
        var entry = MakeEntry();
        var saving = service.SetDoneAsync(entry, isDone: true);

        var act = () => service.DeleteAsync(entry);

        await act.Should().ThrowAsync<EntryBusyException>();
        gate.SetResult();
        await saving;
    }

    [Fact]
    public async Task A_failed_deletion_leaves_the_entry_fully_workable()
    {
        this.repo.DeleteAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntryDeletionException("„x.pdf“ ist in einem anderen Programm geöffnet."));
        var service = this.CreateService();
        var entry = MakeEntry();

        var delete = () => service.DeleteAsync(entry);
        await delete.Should().ThrowAsync<EntryDeletionException>();
        await this.overview.DidNotReceive().RegenerateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());

        var updated = await service.GenerateSectionAsync(entry, BuiltInSections.TaskList);
        updated.TaskList.Should().Be("TEXT");
    }

    [Fact]
    public async Task An_overview_failure_after_deleting_is_logged_but_does_not_fail_the_deletion()
    {
        this.overview.RegenerateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new IOException("gesperrt"));
        var service = this.CreateService();
        var entry = MakeEntry();

        var result = await service.DeleteAsync(entry);

        result.Found.Should().BeTrue("the files are already in the trash; reporting failure would be a lie");
        this.logger.Received(1).LogWarning(Arg.Any<string>(), entry.JobId, Arg.Any<IOException>());
    }

    private TaskCompletionSource<string> HoldTheLlm()
    {
        var gate = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
        this.llm.GenerateAsync(Arg.Any<string>(), Arg.Any<string>(), Arg.Any<LlmOptions>(), Arg.Any<CancellationToken>())
            .Returns(_ => gate.Task);
        return gate;
    }

    private EntryProcessingService CreateService()
    {
        var settings = new SettingsHolder(AppSettings.Default, PromptSettings.Default);
        return new EntryProcessingService(
            Substitute.For<IAudioTranscriber>(),
            new SummaryGenerator(this.llm, settings),
            new HeaderParser(),
            this.repo,
            outputRoot: string.Empty,
            overviewService: this.overview,
            settings: settings,
            renderers: [],
            logger: this.logger);
    }

    private static Entry MakeEntry(string jobId = "260922_001_aaaaaaaa", int seq = 1) => new()
    {
        JobId = jobId,
        SequenceNumber = seq,
        CreatedAt = new DateTimeOffset(2026, 9, 22, 9, 30, 0, TimeSpan.FromHours(1)),
        Type = EntryType.Projekt,
        ProjectName = "Johann",
        Title = "Test",
        SourceType = "audio",
        Status = new ProcessingStatus(true, true, false, false, false),
        Transcript = "Ein Transkript.",
    };
}
