namespace Platee.Johann.Infrastructure.Json;

using System.Text.Json;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Services;

/// <summary>
/// Reads and writes Entry records from/to the output directory structure:
///   {OutputRoot}/YYYY-MM-DD/_raw/*_status.json
/// Supports both v1 (Python) and v2 (C#) schemas transparently via JsonMigrator.
/// </summary>
public sealed class JsonRepository : IEntryRepository
{
    /// <summary>Name of the Johann trash folder under the output root (#55).</summary>
    public const string TrashFolderName = EntryTrash.FolderName;

    private readonly string outputRoot;
    private readonly SemaphoreSlim seqLock = new(1, 1);
    private readonly Func<DateTimeOffset> utcNow;

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public JsonRepository(string outputRoot, Func<DateTimeOffset>? utcNow = null)
    {
        this.outputRoot = outputRoot;
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public Task<IReadOnlyList<DateOnly>> GetAvailableDatesAsync(CancellationToken ct = default)
    {
        var dates = new List<DateOnly>();

        if (!Directory.Exists(this.outputRoot))
        {
            return Task.FromResult<IReadOnlyList<DateOnly>>(dates);
        }

        foreach (var dir in Directory.EnumerateDirectories(this.outputRoot))
        {
            var name = Path.GetFileName(dir);

            // A day whose last entry was deleted keeps its folder — and with it the
            // sequence counter, so no number is ever reused — but is no longer listed (#55).
            if (DateOnly.TryParseExact(name, "yyyy-MM-dd", out var date) && HasEntries(dir))
            {
                dates.Add(date);
            }
        }

        dates.Sort((a, b) => b.CompareTo(a)); // newest first
        return Task.FromResult<IReadOnlyList<DateOnly>>(dates);
    }

    private static bool HasEntries(string dayDir)
    {
        var rawDir = Path.Combine(dayDir, "_raw");
        return Directory.Exists(rawDir) && Directory.EnumerateFiles(rawDir, "*_status.json").Any();
    }

    public async Task<IReadOnlyList<Entry>> GetEntriesForDateAsync(
        DateOnly date, CancellationToken ct = default)
    {
        var rawDir = this.GetRawDir(date);
        if (!Directory.Exists(rawDir))
        {
            return [];
        }

        var entries = new List<Entry>();
        foreach (var file in Directory.EnumerateFiles(rawDir, "*_status.json"))
        {
            ct.ThrowIfCancellationRequested();
            var entry = await LoadFileAsync(file, ct);
            if (entry is not null)
            {
                entries.Add(entry);
            }
        }

        return entries.OrderBy(e => e.SequenceNumber).ToList();
    }

    public async Task<Entry?> GetByJobIdAsync(string jobId, CancellationToken ct = default)
    {
        if (!Directory.Exists(this.outputRoot))
        {
            return null;
        }

        // Fast path: parse date prefix from JobId (format: YYMMDD_NNN_XXXXXXXX)
        if (TryParseDateFromJobId(jobId, out var date))
        {
            var rawDir = this.GetRawDir(date);
            var result = await this.ScanDirectoryForJobIdAsync(rawDir, jobId, ct);
            if (result is not null)
            {
                return result;
            }
        }

        // Fallback: full scan for non-standard JobIds or if fast path missed
        foreach (var dir in Directory.EnumerateDirectories(this.outputRoot))
        {
            var rawDir = Path.Combine(dir, "_raw");
            var result = await this.ScanDirectoryForJobIdAsync(rawDir, jobId, ct);
            if (result is not null)
            {
                return result;
            }
        }

        return null;
    }

    private static bool TryParseDateFromJobId(string jobId, out DateOnly date)
    {
        date = default;
        if (jobId.Length < 7 || jobId[6] != '_')
        {
            return false;
        }

        return DateOnly.TryParseExact(
            jobId.AsSpan(0, 6), "yyMMdd",
            System.Globalization.CultureInfo.InvariantCulture,
            System.Globalization.DateTimeStyles.None,
            out date);
    }

    private async Task<Entry?> ScanDirectoryForJobIdAsync(
        string rawDir, string jobId, CancellationToken ct)
    {
        if (!Directory.Exists(rawDir))
        {
            return null;
        }

        foreach (var file in Directory.EnumerateFiles(rawDir, "*_status.json"))
        {
            ct.ThrowIfCancellationRequested();
            var entry = await LoadFileAsync(file, ct);
            if (entry?.JobId == jobId)
            {
                return entry;
            }
        }

        return null;
    }

    public async Task SaveAsync(Entry entry, CancellationToken ct = default)
    {
        var date = DateOnly.FromDateTime(entry.CreatedAt.DateTime);
        var rawDir = this.GetRawDir(date);
        Directory.CreateDirectory(rawDir);

        var dto = EntryMapper.ToDto(entry);
        var filename = FilenameBuilder.Build(entry) + "_status.json";
        var path = Path.Combine(rawDir, filename);

        await using var stream = File.Open(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await JsonSerializer.SerializeAsync(stream, dto, WriteOptions, ct);
    }

    public async Task UpdateAsync(Entry entry, CancellationToken ct = default)
    {
        var lookup = await this.FindStatusFileAsync(entry.JobId, ct);
        if (lookup.Path is null)
        {
            throw lookup.BusyFile is null
                ? new EntryDeletedException()
                : new IOException(
                    $"„{Path.GetFileName(lookup.BusyFile)}“ wird gerade von einem anderen Programm verwendet.");
        }

        // Serialised up front, so a cancellation can never leave a truncated file behind.
        var bytes = JsonSerializer.SerializeToUtf8Bytes(EntryMapper.ToDto(entry), WriteOptions);
        try
        {
            // Truncate, not Create: the file must still exist. Moved to the trash in the
            // meantime — possibly by another Johann — means deleted, not "write it anew" (#55).
            await using var stream = new FileStream(lookup.Path, FileMode.Truncate, FileAccess.Write, FileShare.None);
            await stream.WriteAsync(bytes, CancellationToken.None);
        }
        catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
        {
            throw new EntryDeletedException();
        }
    }

    private static async Task<Entry?> LoadFileAsync(string path, CancellationToken ct)
    {
        await using var stream = File.OpenRead(path);
        var element = await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: ct);
        var dto = JsonMigrator.Migrate(element);
        return EntryMapper.ToDomain(dto);
    }

    /// <summary>
    /// Returns the next sequence number for the given date and immediately persists the
    /// incremented counter to disk — all while holding the lock.  This means the slot is
    /// "reserved" on disk before the lock is released, so no two callers can ever receive
    /// the same number regardless of how fast they call in parallel.
    ///
    /// Counter file: {date}/_raw/_counter.json  →  { "next": N }
    /// On first call for a date the counter is seeded from existing entries for
    /// backward compatibility with entries written before this mechanism existed.
    /// </summary>
    public async Task<int> GetNextSequenceNumberAsync(DateOnly date, CancellationToken ct = default)
    {
        await this.seqLock.WaitAsync(ct);
        try
        {
            var rawDir = this.GetRawDir(date);
            Directory.CreateDirectory(rawDir);

            // The SemaphoreSlim above is instance-scoped. This lock file is what
            // makes the reservation safe when a second Johann process writes to
            // the same output root.
            await using var counterLock = await AcquireCounterLockAsync(rawDir, ct);

            var counterPath = Path.Combine(rawDir, "_counter.json");
            int next;

            if (File.Exists(counterPath))
            {
                await using var rs = File.OpenRead(counterPath);
                var doc = await JsonSerializer.DeserializeAsync<CounterDoc>(rs, ReadOptions, ct);
                next = doc?.Next ?? 1;
            }
            else
            {
                // Seed from existing entries so existing dates stay consistent
                var entries = await this.GetEntriesForDateAsync(date, ct);
                next = entries.Count == 0 ? 1 : entries.Max(e => e.SequenceNumber) + 1;
            }

            // Write incremented value while still inside the lock — this is the key:
            // the reservation is durable before any other caller gets a chance to read.
            // Flush explicitly so the bytes are on disk before the lock file is
            // released, not merely buffered.
            await using (var ws = File.Open(counterPath, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                await JsonSerializer.SerializeAsync(ws, new CounterDoc(next + 1), WriteOptions, ct);
                await ws.FlushAsync(ct);
            }

            return next;
        }
        finally
        {
            this.seqLock.Release();
        }
    }

    /// <summary>
    /// Opens an exclusive lock file, retrying while another process holds it.
    /// This is what makes the counter reservation safe across processes — the
    /// instance-scoped SemaphoreSlim only covers callers inside one process.
    /// </summary>
    private static async Task<FileStream> AcquireCounterLockAsync(string rawDir, CancellationToken ct)
    {
        var lockPath = Path.Combine(rawDir, "_counter.lock");
        var delayMs = 5;

        for (var attempt = 0; ; attempt++)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                return new FileStream(
                    lockPath,
                    FileMode.OpenOrCreate,
                    FileAccess.ReadWrite,
                    FileShare.None,
                    bufferSize: 1,
                    FileOptions.DeleteOnClose);
            }
            catch (IOException) when (attempt < 60)
            {
                await Task.Delay(delayMs, ct);
                delayMs = Math.Min(delayMs * 2, 100);
            }
        }
    }

    public async Task<JobIdMigrationResult> MigrateJobIdsAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(this.outputRoot))
        {
            return JobIdMigrationResult.Empty;
        }

        var migrated = 0;
        var skipped = new List<string>();

        foreach (var dir in Directory.EnumerateDirectories(this.outputRoot))
        {
            var rawDir = Path.Combine(dir, "_raw");
            if (!Directory.Exists(rawDir))
            {
                continue;
            }

            foreach (var file in Directory.EnumerateFiles(rawDir, "*_status.json"))
            {
                ct.ThrowIfCancellationRequested();
                try
                {
                    var entry = await LoadFileAsync(file, ct);
                    if (entry is null)
                    {
                        skipped.Add($"{file}: Datei konnte nicht gelesen werden.");
                        continue;
                    }

                    if (TryParseDateFromJobId(entry.JobId, out _))
                    {
                        continue;
                    }

                    var date = DateOnly.FromDateTime(entry.CreatedAt.DateTime);
                    var newJobId = $"{date:yyMMdd}_{entry.SequenceNumber:D3}_{Guid.NewGuid().ToString("N")[..8]}";
                    var rewritten = entry with { JobId = newJobId };

                    await SaveAsync(rewritten, ct);

                    // Remove old file if SaveAsync wrote to a different path
                    var newPath = Path.Combine(
                        this.GetRawDir(date),
                        FilenameBuilder.Build(rewritten) + "_status.json");
                    if (!string.Equals(Path.GetFullPath(file), Path.GetFullPath(newPath), StringComparison.OrdinalIgnoreCase))
                    {
                        File.Delete(file);
                    }

                    migrated++;
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
                catch (Exception ex)
                {
                    // Keep going with the remaining entries, but report what was
                    // left behind instead of dropping it on the floor (#45 M2).
                    skipped.Add($"{file}: {ex.Message}");
                }
            }
        }

        return new JobIdMigrationResult(migrated, skipped);
    }

    public async Task<EntryDeletionResult> DeleteAsync(string jobId, CancellationToken ct = default)
    {
        var lookup = await this.FindStatusFileAsync(jobId, ct);
        if (lookup.Path is null)
        {
            return lookup.BusyFile is null
                ? EntryDeletionResult.NotFound
                : throw new EntryDeletionException(
                    $"„{Path.GetFileName(lookup.BusyFile)}“ wird gerade von einem anderen Programm verwendet. "
                    + "Es wurde nichts gelöscht; bitte kurz warten und erneut löschen.",
                    lookup.BusyFile);
        }

        // Past this point the move is not cancellable: stopping half way is exactly the
        // partial deletion the trash's roll-back exists to prevent.
        var statusPath = lookup.Path;
        var entry = lookup.Entry!;
        var rawDir = Path.GetDirectoryName(statusPath)!;
        var date = DateOnly.FromDateTime(entry.CreatedAt.DateTime);
        var files = CollectEntryFiles(statusPath, entry);

        return new EntryTrash(this.outputRoot, this.utcNow).Move(entry, date, rawDir, files);
    }

    public Task<TrashPurgeResult> PurgeTrashAsync(DateTimeOffset deletedBefore, CancellationToken ct = default) =>
        Task.FromResult(new EntryTrash(this.outputRoot, this.utcNow).Purge(deletedBefore, ct));

    /// <summary>
    /// Like <see cref="GetByJobIdAsync"/>, but returns the file as well and skips unreadable
    /// neighbours: a corrupt file next to the entry must not make it undeletable.
    /// </summary>
    private async Task<StatusLookup> FindStatusFileAsync(string jobId, CancellationToken ct)
    {
        // A file another process is writing cannot be read. Skipping it like a corrupt file
        // would report "not found" — and a deletion would then claim success (#55). So a busy
        // file is retried briefly and, if it stays busy, reported as such.
        const int Attempts = 20;
        for (var attempt = 1; ; attempt++)
        {
            var lookup = await this.ScanForStatusFileAsync(jobId, ct);
            if (lookup.Path is not null || lookup.BusyFile is null || attempt == Attempts)
            {
                return lookup;
            }

            await Task.Delay(25, ct);
        }
    }

    private async Task<StatusLookup> ScanForStatusFileAsync(string jobId, CancellationToken ct)
    {
        if (!Directory.Exists(this.outputRoot))
        {
            return default;
        }

        var rawDirs = new List<string>();
        if (TryParseDateFromJobId(jobId, out var date))
        {
            rawDirs.Add(this.GetRawDir(date));
        }

        rawDirs.AddRange(Directory.EnumerateDirectories(this.outputRoot).Select(d => Path.Combine(d, "_raw")));

        string? busy = null;
        foreach (var rawDir in rawDirs.Distinct(StringComparer.OrdinalIgnoreCase).Where(Directory.Exists))
        {
            foreach (var file in Directory.EnumerateFiles(rawDir, "*_status.json"))
            {
                ct.ThrowIfCancellationRequested();
                Entry? entry;
                try
                {
                    entry = await LoadFileAsync(file, ct);
                }
                catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
                {
                    // Moved away since it was listed — by a deletion, possibly in another process.
                    continue;
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    busy ??= file;
                    continue;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // Corrupt neighbour: it must not make this entry undeletable.
                    continue;
                }

                if (entry?.JobId == jobId)
                {
                    return new StatusLookup(file, entry, null);
                }
            }
        }

        return new StatusLookup(null, null, busy);
    }

    /// <summary>
    /// Every file of the entry, matched by exact name — never by wildcard, which would also
    /// catch longer names sharing the prefix. The stem is taken from the status file actually
    /// found (legacy entries may be named differently) and from <see cref="FilenameBuilder"/>,
    /// which today's renderers use. The status file comes last, so an interrupted run leaves
    /// the entry in place rather than orphaned artefacts without an entry.
    /// </summary>
    private static List<string> CollectEntryFiles(string statusPath, Entry entry)
    {
        const string StatusSuffix = "_status.json";
        var rawDir = Path.GetDirectoryName(statusPath)!;
        var dayDir = Path.GetDirectoryName(rawDir)!;
        var statusName = Path.GetFileName(statusPath);
        var stems = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            statusName[..^StatusSuffix.Length],
            FilenameBuilder.Build(entry),
        };

        bool BelongsToEntry(string file)
        {
            var name = Path.GetFileName(file);
            return !name.StartsWith('_')
                && !name.EndsWith(StatusSuffix, StringComparison.OrdinalIgnoreCase)
                && (stems.Contains(Path.GetFileNameWithoutExtension(name))
                    || stems.Any(s => name.Equals(s + "_email.txt", StringComparison.OrdinalIgnoreCase)));
        }

        var files = Directory.EnumerateFiles(dayDir).Where(BelongsToEntry)
            .Concat(Directory.EnumerateFiles(rawDir).Where(BelongsToEntry))
            .ToList();
        files.Add(statusPath);
        return files;
    }

    private string GetRawDir(DateOnly date) =>
        Path.Combine(this.outputRoot, date.ToString("yyyy-MM-dd"), "_raw");

    /// <summary>Where an entry's status file is — or which file could not be read to tell.</summary>
    private readonly record struct StatusLookup(string? Path, Entry? Entry, string? BusyFile);
}

file sealed record CounterDoc(int Next);

