namespace Platee.Johann.Application.Interfaces;

using Platee.Johann.Domain.Entities;

/// <summary>
/// Outcome of a JobId migration run. <paramref name="Skipped"/> is never silently
/// dropped: a file the migration could not rewrite stays on the slow lookup path
/// forever, so the caller has to be able to see it (#45 M2).
/// </summary>
public sealed record JobIdMigrationResult(int Migrated, IReadOnlyList<string> Skipped)
{
    public static readonly JobIdMigrationResult Empty = new(0, []);
}

public interface IEntryRepository
{
    Task<IReadOnlyList<DateOnly>> GetAvailableDatesAsync(CancellationToken ct = default);

    Task<IReadOnlyList<Entry>> GetEntriesForDateAsync(DateOnly date, CancellationToken ct = default);

    Task<Entry?> GetByJobIdAsync(string jobId, CancellationToken ct = default);

    Task SaveAsync(Entry entry, CancellationToken ct = default);

    Task<int> GetNextSequenceNumberAsync(DateOnly date, CancellationToken ct = default);

    Task<JobIdMigrationResult> MigrateJobIdsAsync(CancellationToken ct = default);
}
