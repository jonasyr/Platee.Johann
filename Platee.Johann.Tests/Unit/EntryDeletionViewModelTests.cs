namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using NSubstitute.ExceptionExtensions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.Infrastructure.Audio;
using Platee.Johann.UI.ViewModels;

/// <summary>
/// Einträge löschen (#55) in der Oberfläche: Rückfrage, Auswahl danach, Tage ohne
/// Einträge, Fehlermeldungen. Das Repository ist ein kleiner Speicher im Speicher, damit
/// Liste, Zählung und Tagesliste nach dem Löschen wirklich neu gelesen werden.
/// </summary>
public sealed class EntryDeletionViewModelTests
{
    private static readonly DateOnly Newer = new(2026, 9, 22);
    private static readonly DateOnly Middle = new(2026, 9, 21);
    private static readonly DateOnly Older = new(2026, 9, 20);

    private readonly Dictionary<DateOnly, List<Entry>> store = [];
    private readonly IEntryRepository repository = Substitute.For<IEntryRepository>();
    private readonly IEntryProcessor processor = Substitute.For<IEntryProcessor>();
    private readonly List<Entry> confirmed = [];

    public EntryDeletionViewModelTests()
    {
        this.repository.GetAvailableDatesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => this.store.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key)
                .OrderByDescending(d => d).ToList());
        this.repository.GetEntriesForDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(call => this.store.TryGetValue(call.Arg<DateOnly>(), out var list)
                ? list.ToList()
                : new List<Entry>());
        this.processor.DeleteAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var entry = call.Arg<Entry>();
                this.store[DateOnly.FromDateTime(entry.CreatedAt.DateTime)].RemoveAll(e => e.JobId == entry.JobId);
                return new EntryDeletionResult(true, @"C:\out\_Papierkorb\x", [entry.JobId]);
            });
        this.processor.SetDoneAsync(Arg.Any<Entry>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call => call.Arg<Entry>() with { IsDone = call.Arg<bool>() });
    }

    [Fact]
    public async Task Without_confirmation_nothing_is_deleted()
    {
        var vm = await this.CreateVmAsync(Newer, 3, confirm: false);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        await this.processor.DidNotReceive().DeleteAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
        vm.Entries.Should().HaveCount(3);
        this.confirmed.Should().ContainSingle("the user was asked");
    }

    [Fact]
    public async Task Without_a_confirmation_hook_nothing_is_deleted()
    {
        var vm = await this.CreateVmAsync(Newer, 3, confirm: null);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        await this.processor.DidNotReceive().DeleteAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Deleting_the_selected_entry_selects_the_next_one()
    {
        var vm = await this.CreateVmAsync(Newer, 3);
        vm.SelectedEntry = vm.Entries[1];

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        await this.processor.Received(1).DeleteAsync(
            Arg.Is<Entry>(e => e.SequenceNumber == 2), Arg.Any<CancellationToken>());
        vm.Entries.Select(r => r.SequenceNumber).Should().Equal(1, 3);
        vm.SelectedEntry!.SequenceNumber.Should().Be(3);
        vm.Detail.Entry!.SequenceNumber.Should().Be(3, "the detail view follows the selection");
    }

    [Fact]
    public async Task Deleting_the_last_row_selects_the_previous_one()
    {
        var vm = await this.CreateVmAsync(Newer, 3);
        vm.SelectedEntry = vm.Entries[2];

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        vm.SelectedEntry!.SequenceNumber.Should().Be(2);
    }

    [Fact]
    public async Task Deleting_another_row_keeps_the_selection()
    {
        var vm = await this.CreateVmAsync(Newer, 3);
        var selected = vm.Entries[0];
        vm.SelectedEntry = selected;

        await vm.DeleteEntryCommand.ExecuteAsync(vm.Entries[2]);

        await this.processor.Received(1).DeleteAsync(
            Arg.Is<Entry>(e => e.SequenceNumber == 3), Arg.Any<CancellationToken>());
        vm.SelectedEntry.Should().BeSameAs(selected);
        this.confirmed.Should().ContainSingle().Which.SequenceNumber.Should().Be(3,
            "the confirmation must name the row that is deleted, not the selected one");
    }

    [Fact]
    public async Task Counts_on_the_day_follow_the_deletion()
    {
        var vm = await this.CreateVmAsync(Newer, 3);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        var day = vm.AvailableDates.Single(d => d.Date == Newer);
        day.TotalCount.Should().Be(2);
        day.PendingCount.Should().Be(2);
    }

    [Fact]
    public async Task Deleting_the_last_entry_of_a_day_removes_the_day_and_opens_the_next_older_one()
    {
        this.Seed(Newer, 2);
        this.Seed(Older, 1);
        var vm = await this.CreateVmAsync(Middle, 1);
        vm.SelectedDateItem = vm.AvailableDates.Single(d => d.Date == Middle);
        await WaitForEntriesAsync(vm, Middle);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        vm.AvailableDates.Select(d => d.Date).Should().Equal(Newer, Older);
        vm.SelectedDateItem!.Date.Should().Be(Older);
        vm.Entries.Should().ContainSingle();
        vm.SelectedEntry.Should().NotBeNull();
    }

    [Fact]
    public async Task Deleting_the_last_entry_of_the_oldest_day_opens_the_next_newer_one()
    {
        this.Seed(Newer, 2);
        var vm = await this.CreateVmAsync(Older, 1);
        vm.SelectedDateItem = vm.AvailableDates.Single(d => d.Date == Older);
        await WaitForEntriesAsync(vm, Older);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        vm.AvailableDates.Select(d => d.Date).Should().Equal(Newer);
        vm.SelectedDateItem!.Date.Should().Be(Newer);
        vm.Entries.Should().HaveCount(2);
    }

    [Fact]
    public async Task Deleting_the_very_last_entry_leaves_an_empty_view()
    {
        var vm = await this.CreateVmAsync(Newer, 1);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        vm.AvailableDates.Should().BeEmpty();
        vm.SelectedDateItem.Should().BeNull();
        vm.Entries.Should().BeEmpty();
        vm.Detail.Entry.Should().BeNull();
    }

    [Fact]
    public async Task A_day_with_done_entries_left_stays_when_only_pending_ones_are_shown()
    {
        this.store[Newer] = [MakeEntry(Newer, 1), MakeEntry(Newer, 2) with { IsDone = true }];
        var vm = await this.CreateVmAsync(date: null, count: 0);
        vm.ShowOnlyPending = true;
        await WaitForEntriesAsync(vm, Newer);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        vm.AvailableDates.Should().ContainSingle(d => d.Date == Newer);
        vm.Entries.Should().BeEmpty();
    }

    [Fact]
    public async Task A_failed_deletion_keeps_the_row_and_says_why()
    {
        this.processor.DeleteAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntryDeletionException("„x.pdf“ ist in einem anderen Programm geöffnet. Es wurde nichts gelöscht."));
        var vm = await this.CreateVmAsync(Newer, 2);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        vm.Entries.Should().HaveCount(2);
        vm.Toasts.Toasts.Should().ContainSingle(t => t.Tone == ToastTone.Error && t.Title.Contains("x.pdf"));
    }

    [Fact]
    public async Task A_busy_entry_is_not_deleted_and_the_user_is_told()
    {
        this.processor.DeleteAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntryBusyException());
        var vm = await this.CreateVmAsync(Newer, 2);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        vm.Entries.Should().HaveCount(2);
        vm.Toasts.Toasts.Should().ContainSingle(t => t.Tone == ToastTone.Warn);
    }

    [Fact]
    public async Task While_the_detail_view_is_still_saving_the_entry_it_is_not_deleted()
    {
        var gate = new TaskCompletionSource<Entry>();
        this.processor.SetDoneAsync(Arg.Any<Entry>(), Arg.Any<bool>(), Arg.Any<CancellationToken>()).Returns(gate.Task);
        var vm = await this.CreateVmAsync(Newer, 2);
        var toggling = vm.Detail.ToggleDoneCommand.ExecuteAsync(null);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        this.confirmed.Should().BeEmpty("there is no point asking while the delete would be refused");
        await this.processor.DidNotReceive().DeleteAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
        vm.Toasts.Toasts.Should().ContainSingle(t => t.Tone == ToastTone.Warn);
        gate.SetResult(vm.Detail.Entry! with { IsDone = true });
        await toggling;
    }

    [Fact]
    public async Task The_done_flag_is_saved_through_the_processor_guard()
    {
        // Review PR #55: saved straight to the repository, "erledigt" bypassed the guard and
        // could write a deleted entry back.
        var vm = await this.CreateVmAsync(Newer, 1);

        await vm.Detail.ToggleDoneCommand.ExecuteAsync(null);

        await this.processor.Received(1).SetDoneAsync(Arg.Any<Entry>(), true, Arg.Any<CancellationToken>());
        await this.repository.DidNotReceive().SaveAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task While_an_entry_is_being_deleted_list_and_detail_are_locked()
    {
        var gate = new TaskCompletionSource<EntryDeletionResult>();
        this.processor.DeleteAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>()).Returns(gate.Task);
        var vm = await this.CreateVmAsync(Newer, 2);

        var deleting = vm.DeleteEntryCommand.ExecuteAsync(null);

        vm.IsDeletingEntry.Should().BeTrue("no click may reach the entry while its files are moving");
        gate.SetResult(new EntryDeletionResult(true, @"C:\out\_Papierkorb\x", []));
        await deleting;
        vm.IsDeletingEntry.Should().BeFalse();
    }

    [Fact]
    public async Task The_lock_is_released_after_a_failed_deletion()
    {
        this.processor.DeleteAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntryDeletionException("gesperrt"));
        var vm = await this.CreateVmAsync(Newer, 2);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        vm.IsDeletingEntry.Should().BeFalse();
    }

    [Fact]
    public async Task A_refused_done_save_is_reported_instead_of_crashing()
    {
        this.processor.SetDoneAsync(Arg.Any<Entry>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .ThrowsAsync(new EntryDeletedException());
        var log = new List<string>();
        var detail = new EntryDetailViewModel(
            [],
            string.Empty,
            this.processor,
            this.repository,
            addLog: (message, running) =>
            {
                log.Add(message);
                return new ProcessLogItem(message, DateTime.Now, running);
            })
        {
            Entry = MakeEntry(Newer, 1),
        };

        var act = () => detail.ToggleDoneCommand.ExecuteAsync(null);

        await act.Should().NotThrowAsync();
        log.Should().ContainSingle(m => m.StartsWith("Fehler:") && m.Contains("gelöscht"));
        detail.Entry!.IsDone.Should().BeFalse("nothing was saved");
    }

    [Fact]
    public async Task An_entry_already_gone_is_not_reported_as_deleted()
    {
        // Codex, PR #98: removed externally (Explorer, another Johann), nothing was moved to
        // the trash — claiming "Gelöscht" would hide leftover files from the user.
        this.processor.DeleteAsync(Arg.Any<Entry>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var entry = call.Arg<Entry>();
                this.store[Newer].RemoveAll(e => e.JobId == entry.JobId);
                return EntryDeletionResult.NotFound;
            });
        var vm = await this.CreateVmAsync(Newer, 2);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        vm.Toasts.Toasts.Should().NotContain(t => t.Title.StartsWith("✓"));
        vm.Toasts.Toasts.Should().ContainSingle(t => t.Tone == ToastTone.Warn && t.Title.Contains("nicht mehr vorhanden"));
        vm.Entries.Should().ContainSingle("the list follows the store");
    }

    [Fact]
    public async Task A_successful_deletion_is_confirmed_and_logged()
    {
        var vm = await this.CreateVmAsync(Newer, 2);

        await vm.DeleteEntryCommand.ExecuteAsync(null);

        vm.Toasts.Toasts.Should().ContainSingle(t => t.Tone == ToastTone.Ok && t.Title.Contains("Titel 1"));
        vm.ProcessLog.Should().ContainSingle(l => l.Message.Contains("Titel 1"));
    }

    [Fact]
    public async Task The_command_needs_an_entry()
    {
        var vm = await this.CreateVmAsync(Newer, 1);
        vm.SelectedEntry = null;

        vm.DeleteEntryCommand.CanExecute(null).Should().BeFalse();
        vm.DeleteEntryCommand.CanExecute(new EntryRowViewModel(MakeEntry(Newer, 1))).Should().BeTrue();
    }

    [Fact]
    public void Confirmation_text_names_the_entry_and_what_happens_to_it()
    {
        var entry = MakeEntry(Newer, 7) with { ProjectName = "Allgemein", Title = "Angebot prüfen" };

        var text = EntryDeletionPrompt.MessageFor(entry, @"C:\out\_Papierkorb");

        text.Should().Contain("007").And.Contain("Allgemein").And.Contain("Angebot prüfen");
        text.Should().Contain(@"C:\out\_Papierkorb").And.Contain("30 Tage");
        text.Should().Contain("Archiv", "the original recording stays and the user should know it");
    }

    // ── Hilfen ────────────────────────────────────────────────────────────────
    private void Seed(DateOnly date, int count) =>
        this.store[date] = Enumerable.Range(1, count).Select(i => MakeEntry(date, i)).ToList();

    private async Task<MainViewModel> CreateVmAsync(DateOnly? date, int count, bool? confirm = true)
    {
        if (date is not null)
        {
            this.Seed(date.Value, count);
        }

        var holder = new SettingsHolder(new AppSettings(), PromptSettings.Default);
        var vm = new MainViewModel(
            this.repository,
            [],
            string.Empty,
            this.processor,
            Substitute.For<ISettingsRepository>(),
            Substitute.For<IPromptSettingsRepository>(),
            holder,
            holder,
            new NoOpMicrophoneRecorder());
        if (confirm is not null)
        {
            vm.ConfirmDeleteEntry = entry =>
            {
                this.confirmed.Add(entry);
                return confirm.Value;
            };
        }

        await vm.InitializeAsync();
        return vm;
    }

    /// <summary>Selecting a date loads its entries fire-and-forget; wait until they are in.</summary>
    private static async Task WaitForEntriesAsync(MainViewModel vm, DateOnly date)
    {
        for (var i = 0; i < 100 && (vm.SelectedDateItem?.Date != date || vm.IsLoading); i++)
        {
            await Task.Delay(10);
        }

        await Task.Delay(20);
    }

    private static Entry MakeEntry(DateOnly date, int seq) => new()
    {
        JobId = $"{date:yyMMdd}_{seq:D3}_{seq:D8}",
        SequenceNumber = seq,
        CreatedAt = new DateTimeOffset(date.Year, date.Month, date.Day, 9, seq, 0, TimeSpan.FromHours(1)),
        Type = EntryType.Projekt,
        ProjectName = "Johann",
        Title = $"Titel {seq}",
        SourceType = "audio",
        Status = new ProcessingStatus(true, true, false, false, false),
        Transcript = "Ein Transkript.",
    };
}
