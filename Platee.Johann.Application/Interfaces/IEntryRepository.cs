using Platee.Johann.Domain.Entities;

namespace Platee.Johann.Application.Interfaces;

public interface IEntryRepository
{
    Task<IReadOnlyList<DateOnly>> GetAvailableDatesAsync(CancellationToken ct = default);
    Task<IReadOnlyList<Entry>> GetEntriesForDateAsync(DateOnly date, CancellationToken ct = default);
    Task<Entry?> GetByJobIdAsync(string jobId, CancellationToken ct = default);
    Task SaveAsync(Entry entry, CancellationToken ct = default);
    Task<int> GetNextSequenceNumberAsync(DateOnly date, CancellationToken ct = default);
}
