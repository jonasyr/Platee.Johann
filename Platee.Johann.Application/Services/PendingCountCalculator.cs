using Platee.Johann.Application.Interfaces;
using Platee.Johann.Domain.Entities;

namespace Platee.Johann.Application.Services;

public static class PendingCountCalculator
{
    public static int CountPending(IEnumerable<Entry> entries)
        => entries.Count(e => !e.IsDone);

    public static async Task<int> GetPendingCountForDateAsync(
        IEntryRepository repository,
        DateOnly date,
        CancellationToken ct = default)
    {
        var entries = await repository.GetEntriesForDateAsync(date, ct);
        return CountPending(entries);
    }
}
