using System.IO;
using System.Text;
using Johann.Application.Interfaces;
using Johann.Domain.Entities;
using Johann.Domain.Parsing;
using Johann.Domain.Services;
using Johann.Domain.ValueObjects;

namespace Johann.Application.Processing;

/// <summary>
/// Orchestrates the full pipeline: transcribe → parse → summarize → save → archive.
/// Mirrors process_single_job / process_text_job from Python main.py.
/// </summary>
public sealed class EntryProcessingService : IEntryProcessor
{
    private readonly IAudioTranscriber      _transcriber;
    private readonly SummaryGenerator       _summaryGenerator;
    private readonly HeaderParser           _parser;
    private readonly IEntryRepository       _repository;
    private readonly string                 _outputRoot;
    private readonly IHtmlOverviewService?  _overviewService;

    public bool CanProcess => _transcriber.IsAvailable;

    public EntryProcessingService(
        IAudioTranscriber      transcriber,
        SummaryGenerator       summaryGenerator,
        HeaderParser           parser,
        IEntryRepository       repository,
        string                 outputRoot    = "",
        IHtmlOverviewService?  overviewService = null)
    {
        _transcriber      = transcriber;
        _summaryGenerator = summaryGenerator;
        _parser           = parser;
        _repository       = repository;
        _outputRoot       = outputRoot;
        _overviewService  = overviewService;
    }

    /// <summary>
    /// Transcribes an MP3 file and generates all summaries, then persists the entry.
    /// </summary>
    public async Task<Entry> ProcessAudioAsync(
        string audioFilePath,
        DateOnly date,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default)
    {
        const int total = 4;

        // Step 1 – Transcription
        progress?.Report(new("Transkribiere Audio…", 1, total));
        var transcription = await _transcriber.TranscribeAsync(audioFilePath, ct);

        // Step 2 – Header parsing + sequence number
        progress?.Report(new("Analysiere Header…", 2, total));
        var header = _parser.Parse(transcription.Transcript);

        var existingEntries = await _repository.GetEntriesForDateAsync(date, ct);
        var seq = existingEntries.Count + 1;

        // Use RemainderText (transcript with type/project tokens stripped) so the
        // title doesn't start with "Aufgabe Johann …" but with the actual content.
        var title = header.ExplicitTitle
            ?? string.Join(" ",
                header.RemainderText
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(5));

        var jobId = BuildJobId(date, seq);
        var now   = DateTime.Now;
        var createdAt = new DateTimeOffset(
            date.Year, date.Month, date.Day, now.Hour, now.Minute, now.Second,
            TimeSpan.FromHours(1));

        var baseEntry = new Entry
        {
            JobId           = jobId,
            SequenceNumber  = seq,
            Type            = header.Type,
            ProjectName     = header.ProjectName,
            Title           = title,
            CreatedAt       = createdAt,
            SourceType      = "audio",
            Status          = new ProcessingStatus(
                Transcribed:  true,
                Summarized:   false,
                PdfCreated:   false,
                Archived:     false,
                EmailCreated: false),
            Transcript      = transcription.Transcript,
            DurationSeconds = transcription.DurationSeconds,
            WordCount       = transcription.WordCount,
        };

        // Step 3 – Summaries (parallel for speed)
        progress?.Report(new("Generiere Zusammenfassungen…", 3, total));
        var (abstractText, longSummary, proseSummary) =
            await GenerateSummariesAsync(transcription.Transcript, ct);

        var finalEntry = baseEntry with
        {
            Abstract     = string.IsNullOrEmpty(abstractText) ? null : abstractText,
            LongSummary  = string.IsNullOrEmpty(longSummary)  ? null : longSummary,
            ProseSummary = string.IsNullOrEmpty(proseSummary) ? null : proseSummary,
            Status       = new ProcessingStatus(
                Transcribed:  true,
                Summarized:   true,
                PdfCreated:   false,
                Archived:     false,
                EmailCreated: false),
        };

        // Step 4 – Persist JSON + archive raw files + regenerate overview
        progress?.Report(new("Speichern…", 4, total));
        await _repository.SaveAsync(finalEntry, ct);
        await ArchiveRawFilesAsync(audioFilePath, finalEntry, ct);

        if (_overviewService is not null)
            await _overviewService.RegenerateAsync(date, ct);

        return finalEntry;
    }

    /// <summary>
    /// Re-generates all summaries from an existing transcript and saves the updated entry.
    /// </summary>
    public async Task<Entry> ReprocessAsync(
        Entry entry,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entry.Transcript))
            throw new InvalidOperationException(
                "Kein Transkript vorhanden – kann nicht neu verarbeiten.");

        const int total = 2;

        // Step 1 – Summaries
        progress?.Report(new("Generiere Zusammenfassungen…", 1, total));
        var (abstractText, longSummary, proseSummary) =
            await GenerateSummariesAsync(entry.Transcript, ct);

        var updatedEntry = entry with
        {
            Abstract     = string.IsNullOrEmpty(abstractText) ? entry.Abstract     : abstractText,
            LongSummary  = string.IsNullOrEmpty(longSummary)  ? entry.LongSummary  : longSummary,
            ProseSummary = string.IsNullOrEmpty(proseSummary) ? entry.ProseSummary : proseSummary,
            Status       = entry.Status with { Summarized = true },
        };

        // Step 2 – Persist + regenerate overview
        progress?.Report(new("Speichern…", 2, total));
        await _repository.SaveAsync(updatedEntry, ct);

        if (_overviewService is not null)
        {
            var date = DateOnly.FromDateTime(updatedEntry.CreatedAt.DateTime);
            await _overviewService.RegenerateAsync(date, ct);
        }

        return updatedEntry;
    }

    /// <summary>
    /// Generates an email text for the entry via GPT (using ProseSummary as source),
    /// or falls back to a simple plain-text composition when GPT is unavailable.
    /// </summary>
    public async Task<string> GenerateEmailTextAsync(
        Entry entry,
        CancellationToken ct = default)
    {
        // Prefer ProseSummary, then LongSummary, then Abstract as GPT input
        var source = entry.ProseSummary
            ?? entry.LongSummary
            ?? entry.Abstract
            ?? string.Empty;

        if (_summaryGenerator.IsAvailable && !string.IsNullOrWhiteSpace(source))
            return await _summaryGenerator.GenerateEmailTextAsync(source, ct);

        // Fallback: compose from available content without GPT
        return BuildFallbackEmailText(entry);
    }

    // ── Private helpers ───────────────────────────────────────────────────────

    private async Task<(string Abstract, string LongSummary, string ProseSummary)>
        GenerateSummariesAsync(string transcript, CancellationToken ct)
    {
        var abstractTask = _summaryGenerator.GenerateAbstractAsync(transcript, ct);
        var longTask     = _summaryGenerator.GenerateLongSummaryAsync(transcript, ct);
        var proseTask    = _summaryGenerator.GenerateProseSummaryAsync(transcript, ct);

        await Task.WhenAll(abstractTask, longTask, proseTask);

        return (abstractTask.Result, longTask.Result, proseTask.Result);
    }

    /// <summary>
    /// Copies the source audio file and writes the transcript text into
    /// {outputRoot}/{YYYY-MM-DD}/_raw/ using the FilenameBuilder convention.
    /// Failures are swallowed — archival is non-critical.
    /// </summary>
    private async Task ArchiveRawFilesAsync(string sourceAudioPath, Entry entry, CancellationToken ct)
    {
        if (string.IsNullOrEmpty(_outputRoot)) return;

        try
        {
            var date   = DateOnly.FromDateTime(entry.CreatedAt.DateTime);
            var rawDir = Path.Combine(_outputRoot, date.ToString("yyyy-MM-dd"), "_raw");
            Directory.CreateDirectory(rawDir);

            var baseName = FilenameBuilder.Build(entry);

            var audioExt  = Path.GetExtension(sourceAudioPath);
            var audioDest = Path.Combine(rawDir, baseName + audioExt);
            if (!File.Exists(audioDest))
                File.Copy(sourceAudioPath, audioDest);

            if (!string.IsNullOrEmpty(entry.Transcript))
            {
                var txtPath = Path.Combine(rawDir, baseName + ".txt");
                await File.WriteAllTextAsync(txtPath, entry.Transcript, ct);
            }
        }
        catch
        {
            // Non-critical: archival failure must not break the pipeline
        }
    }

    private static string BuildFallbackEmailText(Entry entry)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Betreff: {entry.ProjectName}: {entry.Title}");
        sb.AppendLine(new string('-', 60));
        sb.AppendLine();

        if (!string.IsNullOrWhiteSpace(entry.ProseSummary))
            sb.AppendLine(entry.ProseSummary);
        else if (!string.IsNullOrWhiteSpace(entry.Abstract))
            sb.AppendLine(entry.Abstract);

        sb.AppendLine();
        sb.AppendLine($"[{entry.CreatedAt:dd.MM.yyyy} · {entry.ProjectName}]");
        return sb.ToString();
    }

    private static string BuildJobId(DateOnly date, int seq)
        => $"{date:yyMMdd}_{seq:D3}_{Guid.NewGuid().ToString("N")[..8]}";
}
