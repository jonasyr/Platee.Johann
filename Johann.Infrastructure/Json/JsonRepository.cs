using System.Text.Json;
using Johann.Application.Interfaces;
using Johann.Domain.Entities;

namespace Johann.Infrastructure.Json;

/// <summary>
/// Reads and writes Entry records from/to the output directory structure:
///   {OutputRoot}/YYYY-MM-DD/_raw/*_status.json
/// Supports both v1 (Python) and v2 (C#) schemas transparently via JsonMigrator.
/// </summary>
public sealed class JsonRepository : IEntryRepository
{
    private readonly string _outputRoot;

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
        _outputRoot = outputRoot;
    }

    public Task<IReadOnlyList<DateOnly>> GetAvailableDatesAsync(CancellationToken ct = default)
    {
        var dates = new List<DateOnly>();

        if (!Directory.Exists(_outputRoot))
            return Task.FromResult<IReadOnlyList<DateOnly>>(dates);

        foreach (var dir in Directory.EnumerateDirectories(_outputRoot))
        {
            var name = Path.GetFileName(dir);
            if (DateOnly.TryParseExact(name, "yyyy-MM-dd", out var date))
                dates.Add(date);
        }

        dates.Sort((a, b) => b.CompareTo(a)); // newest first
        return Task.FromResult<IReadOnlyList<DateOnly>>(dates);
    }

    public async Task<IReadOnlyList<Entry>> GetEntriesForDateAsync(
        DateOnly date, CancellationToken ct = default)
    {
        var rawDir = GetRawDir(date);
        if (!Directory.Exists(rawDir))
            return [];

        var entries = new List<Entry>();
        foreach (var file in Directory.EnumerateFiles(rawDir, "*_status.json"))
        {
            ct.ThrowIfCancellationRequested();
            var entry = await LoadFileAsync(file, ct);
            if (entry is not null)
                entries.Add(entry);
        }

        return entries.OrderBy(e => e.SequenceNumber).ToList();
    }

    public async Task<Entry?> GetByJobIdAsync(string jobId, CancellationToken ct = default)
    {
        if (!Directory.Exists(_outputRoot))
            return null;

        foreach (var dir in Directory.EnumerateDirectories(_outputRoot))
        {
            var rawDir = Path.Combine(dir, "_raw");
            if (!Directory.Exists(rawDir)) continue;

            foreach (var file in Directory.EnumerateFiles(rawDir, "*_status.json"))
            {
                ct.ThrowIfCancellationRequested();
                var entry = await LoadFileAsync(file, ct);
                if (entry?.JobId == jobId)
                    return entry;
            }
        }
        return null;
    }

    public async Task SaveAsync(Entry entry, CancellationToken ct = default)
    {
        var date = DateOnly.FromDateTime(entry.CreatedAt.DateTime);
        var rawDir = GetRawDir(date);
        Directory.CreateDirectory(rawDir);

        var dto = EntryMapper.ToDto(entry);
        var filename = $"{entry.JobId}_status.json";
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

    private string GetRawDir(DateOnly date) =>
        Path.Combine(_outputRoot, date.ToString("yyyy-MM-dd"), "_raw");
}
