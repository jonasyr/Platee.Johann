namespace Platee.Johann.Tests.Unit;

using System.ComponentModel;
using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.Infrastructure.Audio;
using Platee.Johann.UI.ViewModels;

/// <summary>
/// Sortieren, „erledigt“ und der Filter laden die Liste nicht mehr neu (#100): vorhandene
/// Zeilen bleiben dieselben Objekte, die Auswahl bleibt, und nur der neueste Ladevorgang
/// zählt. Das Repository ist ein kleiner Speicher im Speicher; einzelne Tage lassen sich
/// „langsam“ schalten, um den Wettlauf zweier Ladevorgänge nachzustellen.
/// </summary>
public sealed class EntryListReconcileTests
{
    private static readonly DateOnly Newer = new(2026, 9, 22);
    private static readonly DateOnly Older = new(2026, 9, 20);

    private readonly Dictionary<DateOnly, List<Entry>> store = [];
    private readonly Dictionary<DateOnly, TaskCompletionSource<IReadOnlyList<Entry>>> slowDays = [];
    private readonly IEntryRepository repository = Substitute.For<IEntryRepository>();
    private readonly IEntryProcessor processor = Substitute.For<IEntryProcessor>();

    public EntryListReconcileTests()
    {
        this.repository.GetAvailableDatesAsync(Arg.Any<CancellationToken>())
            .Returns(_ => this.store.Where(kv => kv.Value.Count > 0).Select(kv => kv.Key)
                .OrderByDescending(d => d).ToList());
        this.repository.GetEntriesForDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var date = call.Arg<DateOnly>();
                if (this.slowDays.Remove(date, out var slow))
                {
                    return slow.Task;
                }

                IReadOnlyList<Entry> entries = this.store.TryGetValue(date, out var list) ? list.ToList() : [];
                return Task.FromResult(entries);
            });
        this.processor.SetDoneAsync(Arg.Any<Entry>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(call =>
            {
                var updated = call.Arg<Entry>() with { IsDone = call.Arg<bool>() };
                var day = this.store[DateOnly.FromDateTime(updated.CreatedAt.DateTime)];
                day[day.FindIndex(e => e.JobId == updated.JobId)] = updated;
                return updated;
            });
    }

    // ── Sortieren ─────────────────────────────────────────────────────────────
    [Fact]
    public async Task Sorting_reorders_the_rows_in_memory_and_keeps_the_selected_object()
    {
        var vm = await this.CreateVmAsync(Newer, 3);
        var selected = vm.Entries[1];
        vm.SelectedEntry = selected;
        var loading = TrackLoading(vm);
        this.repository.ClearReceivedCalls();

        vm.SortByIdCommand.Execute(null); // already by id: reverses

        vm.Entries.Select(r => r.SequenceNumber).Should().Equal(3, 2, 1);
        vm.SelectedEntry.Should().BeSameAs(selected);
        await this.repository.DidNotReceive().GetEntriesForDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
        loading.Should().BeEmpty("sorting must never show the loading overlay");
    }

    [Fact]
    public async Task Sorting_by_project_orders_by_project_then_number()
    {
        this.store[Newer] =
        [
            MakeEntry(Newer, 1) with { ProjectName = "Zeta" },
            MakeEntry(Newer, 2) with { ProjectName = "Alpha" },
            MakeEntry(Newer, 3) with { ProjectName = "Alpha" },
        ];
        var vm = await this.CreateVmAsync(date: null, count: 0);
        var rows = vm.Entries.ToList();

        vm.SortByProjectCommand.Execute(null);

        vm.Entries.Select(r => r.SequenceNumber).Should().Equal(2, 3, 1);
        vm.Entries.Should().OnlyContain(r => rows.Contains(r), "the rows are moved, not rebuilt");
    }

    [Fact]
    public async Task Sorting_keeps_the_section_ticks_the_user_set()
    {
        var vm = await this.CreateVmAsync(Newer, 3);
        vm.Sections.ShowConversationNote = true; // not the default for a project entry

        vm.SortByIdCommand.Execute(null);

        vm.Sections.ShowConversationNote.Should().BeTrue();
    }

    // ── Erledigt ──────────────────────────────────────────────────────────────
    [Fact]
    public async Task Marking_done_keeps_the_selection_and_updates_the_counts_without_reloading()
    {
        var vm = await this.CreateVmAsync(Newer, 3);
        var selected = vm.Entries[1];
        vm.SelectedEntry = selected;
        var loading = TrackLoading(vm);
        this.repository.ClearReceivedCalls();

        await vm.Detail.ToggleDoneCommand.ExecuteAsync(null);

        vm.SelectedEntry.Should().BeSameAs(selected);
        selected.IsDone.Should().BeTrue();
        vm.Entries.Should().HaveCount(3);
        var day = vm.AvailableDates.Single(d => d.Date == Newer);
        day.TotalCount.Should().Be(3);
        day.PendingCount.Should().Be(2);
        await this.repository.DidNotReceive().GetEntriesForDateAsync(Arg.Any<DateOnly>(), Arg.Any<CancellationToken>());
        loading.Should().BeEmpty();
    }

    [Fact]
    public async Task Two_overlapping_done_saves_of_one_entry_count_once()
    {
        // Review #100: the count is adjusted, no longer re-read. Two saves that both report
        // "done" (the command started twice before the first returned) must not count twice.
        var vm = await this.CreateVmAsync(Newer, 3);
        var gate = new TaskCompletionSource();
        this.processor.SetDoneAsync(Arg.Any<Entry>(), Arg.Any<bool>(), Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await gate.Task;
                return call.Arg<Entry>() with { IsDone = call.Arg<bool>() };
            });

        var first = vm.Detail.ToggleDoneCommand.ExecuteAsync(null);
        var second = vm.Detail.ToggleDoneCommand.ExecuteAsync(null);
        gate.SetResult();
        await Task.WhenAll(first, second);

        vm.AvailableDates.Single(d => d.Date == Newer).PendingCount.Should().Be(2);
    }

    [Fact]
    public async Task Undoing_done_restores_the_count_and_keeps_the_selection()
    {
        this.store[Newer] = [MakeEntry(Newer, 1), MakeEntry(Newer, 2) with { IsDone = true }];
        var vm = await this.CreateVmAsync(date: null, count: 0);
        var selected = vm.Entries[1];
        vm.SelectedEntry = selected;

        await vm.Detail.ToggleDoneCommand.ExecuteAsync(null);

        vm.SelectedEntry.Should().BeSameAs(selected);
        selected.IsDone.Should().BeFalse();
        vm.AvailableDates.Single(d => d.Date == Newer).PendingCount.Should().Be(2);
    }

    [Fact]
    public async Task Marking_done_with_only_pending_shown_removes_the_row_and_selects_the_neighbour()
    {
        var vm = await this.CreateVmAsync(Newer, 3);
        vm.ShowOnlyPending = true;
        await Settle();
        vm.SelectedEntry = vm.Entries[1];
        var neighbour = vm.Entries[2];

        await vm.Detail.ToggleDoneCommand.ExecuteAsync(null);

        vm.Entries.Select(r => r.SequenceNumber).Should().Equal(1, 3);
        vm.SelectedEntry.Should().BeSameAs(neighbour);
        vm.AvailableDates.Single(d => d.Date == Newer).PendingCount.Should().Be(2);
    }

    [Fact]
    public async Task Marking_the_last_pending_entry_done_leaves_the_day_selected_and_empty()
    {
        var vm = await this.CreateVmAsync(Newer, 1);
        vm.ShowOnlyPending = true;
        await Settle();

        await vm.Detail.ToggleDoneCommand.ExecuteAsync(null);

        vm.SelectedDateItem!.Date.Should().Be(Newer, "the selected day stays visible even when all is done");
        vm.Entries.Should().BeEmpty();
        vm.SelectedEntry.Should().BeNull();
    }

    // ── Filter ────────────────────────────────────────────────────────────────
    [Fact]
    public async Task Switching_the_filter_keeps_the_selected_row_when_it_stays_visible()
    {
        this.store[Newer] = [MakeEntry(Newer, 1) with { IsDone = true }, MakeEntry(Newer, 2), MakeEntry(Newer, 3)];
        var vm = await this.CreateVmAsync(date: null, count: 0);
        var selected = vm.Entries[2];
        vm.SelectedEntry = selected;
        var loading = TrackLoading(vm);

        vm.ShowOnlyPending = true;
        await Settle();

        vm.Entries.Select(r => r.SequenceNumber).Should().Equal(2, 3);
        vm.SelectedEntry.Should().BeSameAs(selected);
        loading.Should().BeEmpty();
    }

    [Fact]
    public async Task Switching_the_filter_selects_the_first_row_when_the_selection_is_hidden()
    {
        this.store[Newer] = [MakeEntry(Newer, 1) with { IsDone = true }, MakeEntry(Newer, 2), MakeEntry(Newer, 3)];
        var vm = await this.CreateVmAsync(date: null, count: 0);
        vm.SelectedEntry = vm.Entries[0];

        vm.ShowOnlyPending = true;
        await Settle();

        vm.SelectedEntry!.SequenceNumber.Should().Be(2);
    }

    [Fact]
    public async Task Switching_the_filter_back_keeps_the_existing_rows()
    {
        this.store[Newer] = [MakeEntry(Newer, 1) with { IsDone = true }, MakeEntry(Newer, 2)];
        var vm = await this.CreateVmAsync(date: null, count: 0);
        vm.ShowOnlyPending = true;
        await Settle();
        var pendingRow = vm.Entries.Single();

        vm.ShowOnlyPending = false;
        await Settle();

        vm.Entries.Select(r => r.SequenceNumber).Should().Equal(1, 2);
        vm.Entries[1].Should().BeSameAs(pendingRow);
        vm.SelectedEntry.Should().BeSameAs(pendingRow);
    }

    // ── Tageswechsel ──────────────────────────────────────────────────────────
    [Fact]
    public async Task While_another_day_loads_the_old_list_stays_and_no_overlay_is_shown()
    {
        this.Seed(Older, 2);
        var vm = await this.CreateVmAsync(Newer, 3);
        var slow = this.MakeSlow(Older);
        var loading = TrackLoading(vm);

        vm.SelectedDateItem = vm.AvailableDates.Single(d => d.Date == Older);

        vm.Entries.Should().HaveCount(3, "nothing is cleared before the new day is read");
        vm.SelectedEntry.Should().NotBeNull();

        slow.SetResult(this.store[Older].ToList());
        await Settle();

        vm.Entries.Select(r => r.Entry.CreatedAt.Date).Should().OnlyContain(d => d == Older.ToDateTime(TimeOnly.MinValue));
        vm.SelectedEntry.Should().BeSameAs(vm.Entries[0]);
        loading.Should().BeEmpty();
    }

    [Fact]
    public async Task An_older_slower_load_does_not_overwrite_a_newer_one()
    {
        this.Seed(Older, 2);
        var vm = await this.CreateVmAsync(Newer, 3);
        var slow = this.MakeSlow(Older);

        vm.SelectedDateItem = vm.AvailableDates.Single(d => d.Date == Older);
        vm.SelectedDateItem = vm.AvailableDates.Single(d => d.Date == Newer);
        await Settle();
        slow.SetResult(this.store[Older].ToList());
        await Settle();

        vm.SelectedDateItem!.Date.Should().Be(Newer);
        vm.Entries.Should().HaveCount(3);
        vm.Entries.Select(r => r.Entry.CreatedAt.Date).Should().OnlyContain(d => d == Newer.ToDateTime(TimeOnly.MinValue));
    }

    // ── Neuer Eintrag ─────────────────────────────────────────────────────────
    [Fact]
    public async Task A_new_entry_is_placed_by_the_current_sort_and_keeps_the_selection()
    {
        var vm = await this.CreateVmAsync(Newer, 2);
        vm.SortByIdCommand.Execute(null); // reversed: 2, 1
        var selected = vm.SelectedEntry;
        var added = MakeEntry(Newer, 3);
        this.store[Newer].Add(added);

        vm.NotifyEntryProcessed(added);
        await Settle();

        vm.Entries.Select(r => r.SequenceNumber).Should().Equal(3, 2, 1);
        vm.SelectedEntry.Should().BeSameAs(selected);
    }

    // ── Hilfen ────────────────────────────────────────────────────────────────
    private void Seed(DateOnly date, int count) =>
        this.store[date] = Enumerable.Range(1, count).Select(i => MakeEntry(date, i)).ToList();

    private TaskCompletionSource<IReadOnlyList<Entry>> MakeSlow(DateOnly date)
    {
        // Continuations run inline: when SetResult returns, the load has finished — no timing.
        var slow = new TaskCompletionSource<IReadOnlyList<Entry>>();
        this.slowDays[date] = slow;
        return slow;
    }

    private async Task<MainViewModel> CreateVmAsync(DateOnly? date, int count)
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
        await vm.InitializeAsync();
        return vm;
    }

    /// <summary>Records every time the loading overlay is switched on.</summary>
    private static List<bool> TrackLoading(MainViewModel vm)
    {
        var shown = new List<bool>();
        ((INotifyPropertyChanged)vm).PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(MainViewModel.IsLoading) && vm.IsLoading)
            {
                shown.Add(true);
            }
        };
        return shown;
    }

    /// <summary>Fire-and-forget continuations (filter, day switch) run on the pool; let them land.</summary>
    private static Task Settle() => Task.Delay(50);

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
