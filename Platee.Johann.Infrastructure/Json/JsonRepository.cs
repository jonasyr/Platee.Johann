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
    private readonly string outputRoot;
    private readonly SemaphoreSlim seqLock = new(1, 1);

    private static readonly JsonSerializerOptions WriteOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private static readonly JsonSerializerOptions ReadOptions = new()
    {
        PropertyNameCaseInsensitive = true,
    };

    public JsonRepository(string outputRoot)
    {
        this.outputRoot = outputRoot;
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
            if (DateOnly.TryParseExact(name, "yyyy-MM-dd", out var date))
            {
                dates.Add(date);
            }
        }

        dates.Sort((a, b) => b.CompareTo(a)); // newest first
        return Task.FromResult<IReadOnlyList<DateOnly>>(dates);
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
            await using var ws = File.Open(counterPath, FileMode.Create, FileAccess.Write, FileShare.None);
            await JsonSerializer.SerializeAsync(ws, new CounterDoc(next + 1), WriteOptions, ct);

            return next;
        }
        finally
        {
            this.seqLock.Release();
        }
    }

    public async Task MigrateJobIdsAsync(CancellationToken ct = default)
    {
        if (!Directory.Exists(this.outputRoot))
        {
            return;
        }

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
                var entry = await LoadFileAsync(file, ct);
                if (entry is null || TryParseDateFromJobId(entry.JobId, out _))
                {
                    continue;
                }

                var date = DateOnly.FromDateTime(entry.CreatedAt.DateTime);
                var newJobId = $"{date:yyMMdd}_{entry.SequenceNumber:D3}_{Guid.NewGuid().ToString("N")[..8]}";
                var migrated = entry with { JobId = newJobId };

                // Delete old file, save with new JobId
                File.Delete(file);
                await SaveAsync(migrated, ct);
            }
        }
    }

    private string GetRawDir(DateOnly date) =>
        Path.Combine(this.outputRoot, date.ToString("yyyy-MM-dd"), "_raw");
}

file sealed record CounterDoc(int Next);
