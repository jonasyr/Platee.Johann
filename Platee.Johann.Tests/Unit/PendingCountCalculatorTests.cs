namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Services;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;

public sealed class PendingCountCalculatorTests
{
    [Fact]
    public async Task GetPendingCountForDateAsync_updates_after_status_change()
    {
        var date = new DateOnly(2026, 3, 31);
        var repo = new FakeEntryRepository();
        repo.SetEntries(
            date,
        [
            MakeEntry("one", date, isDone: false),
            MakeEntry("two", date, isDone: true),
            MakeEntry("three", date, isDone: false),
        ]);

        var pendingBefore = await PendingCountCalculator.GetPendingCountForDateAsync(repo, date);
        pendingBefore.Should().Be(2);

        repo.SetEntries(
            date,
        [
            MakeEntry("one", date, isDone: true),
            MakeEntry("two", date, isDone: true),
            MakeEntry("three", date, isDone: false),
        ]);

        var pendingAfter = await PendingCountCalculator.GetPendingCountForDateAsync(repo, date);
        pendingAfter.Should().Be(1);
    }

    private static Entry MakeEntry(string id, DateOnly date, bool isDone) => new()
    {
        JobId = id,
        SequenceNumber = 1,
        CreatedAt = new DateTimeOffset(date.ToDateTime(TimeOnly.MinValue)),
        ProjectName = "Test",
        Title = "Title",
        SourceType = "text",
        Type = EntryType.Projekt,
        IsDone = isDone,
        Status = ProcessingStatus.Empty,
    };

    private sealed class FakeEntryRepository : IEntryRepository
    {
        private readonly Dictionary<DateOnly, IReadOnlyList<Entry>> entries = [];

        public void SetEntries(DateOnly date, IReadOnlyList<Entry> entries) => this.entries[date] = entries;

        public Task<IReadOnlyList<DateOnly>> GetAvailableDatesAsync(CancellationToken ct = default)
            => Task.FromResult<IReadOnlyList<DateOnly>>(this.entries.Keys.ToList());

        public Task<IReadOnlyList<Entry>> GetEntriesForDateAsync(DateOnly date, CancellationToken ct = default)
            => Task.FromResult(this.entries.TryGetValue(date, out var dateEntries)
                ? dateEntries
                : (IReadOnlyList<Entry>)[]);

        public Task<Entry?> GetByJobIdAsync(string jobId, CancellationToken ct = default)
            => Task.FromResult<Entry?>(this.entries.Values.SelectMany(x => x).FirstOrDefault(x => x.JobId == jobId));

        public Task SaveAsync(Entry entry, CancellationToken ct = default) => Task.CompletedTask;

        public Task<int> GetNextSequenceNumberAsync(DateOnly date, CancellationToken ct = default) => Task.FromResult(1);

        public Task MigrateJobIdsAsync(CancellationToken ct = default) => Task.CompletedTask;
    }
}
