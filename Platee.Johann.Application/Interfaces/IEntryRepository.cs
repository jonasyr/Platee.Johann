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

    /// <summary>Writes the entry; creates its file if needed. Only for new entries.</summary>
    Task SaveAsync(Entry entry, CancellationToken ct = default);

    /// <summary>
    /// Overwrites the status file the entry already has — never creates one (#55). The file
    /// system is the arbiter across Johann processes: a file moved to the trash is not found,
    /// a file open for writing cannot be moved.
    /// </summary>
    /// <exception cref="EntryDeletedException">The entry no longer exists.</exception>
    Task UpdateAsync(Entry entry, CancellationToken ct = default);

    Task<int> GetNextSequenceNumberAsync(DateOnly date, CancellationToken ct = default);

    Task<JobIdMigrationResult> MigrateJobIdsAsync(CancellationToken ct = default);

    /// <summary>
    /// Moves every file of the entry into the Johann trash — all or nothing (#55).
    /// Never touches the sequence counter or the day folder, so a number is never reused.
    /// </summary>
    /// <exception cref="EntryDeletionException">A file could not be moved; nothing changed.</exception>
    Task<EntryDeletionResult> DeleteAsync(string jobId, CancellationToken ct = default);

    /// <summary>
    /// Removes trash folders deleted before <paramref name="deletedBefore"/> for good.
    /// Only folders Johann wrote itself (with their deletion record) are touched.
    /// </summary>
    Task<TrashPurgeResult> PurgeTrashAsync(DateTimeOffset deletedBefore, CancellationToken ct = default);
}
