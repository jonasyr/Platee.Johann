using Johann.Application.Interfaces;
using Johann.Domain.Entities;
using Johann.Domain.Parsing;
using Johann.Domain.ValueObjects;

namespace Johann.Application.Processing;

/// <summary>
/// Orchestrates the full pipeline: transcribe → parse → summarize → save.
/// Mirrors process_single_job / process_text_job from Python main.py.
/// </summary>
public sealed class EntryProcessingService : IEntryProcessor
{
    private readonly IAudioTranscriber _transcriber;
    private readonly SummaryGenerator _summaryGenerator;
    private readonly HeaderParser _parser;
    private readonly IEntryRepository _repository;

    public bool CanProcess => _transcriber.IsAvailable;

    public EntryProcessingService(
        IAudioTranscriber transcriber,
        SummaryGenerator summaryGenerator,
        HeaderParser parser,
        IEntryRepository repository)
    {
        _transcriber = transcriber;
        _summaryGenerator = summaryGenerator;
        _parser = parser;
        _repository = repository;
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

        var title = header.ExplicitTitle
            ?? string.Join(" ",
                transcription.Transcript
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(5));

        var jobId = BuildJobId(date, seq);
        var now = DateTime.Now;
        var createdAt = new DateTimeOffset(
            date.Year, date.Month, date.Day, now.Hour, now.Minute, now.Second,
            TimeSpan.FromHours(1));

        var baseEntry = new Entry
        {
            JobId = jobId,
            SequenceNumber = seq,
            Type = header.Type,
            ProjectName = header.ProjectName,
            Title = title,
            CreatedAt = createdAt,
            SourceType = "audio",
            Status = new ProcessingStatus(
                Transcribed: true,
                Summarized: false,
                PdfCreated: false,
                Archived: false,
                EmailCreated: false),
            Transcript = transcription.Transcript,
            DurationSeconds = transcription.DurationSeconds,
            WordCount = transcription.WordCount,
        };

        // Step 3 – Summaries (parallel for speed)
        progress?.Report(new("Generiere Zusammenfassungen…", 3, total));
        var (abstractText, longSummary, proseSummary) =
            await GenerateSummariesAsync(transcription.Transcript, ct);

        var finalEntry = baseEntry with
        {
            Abstract = string.IsNullOrEmpty(abstractText) ? null : abstractText,
            LongSummary = string.IsNullOrEmpty(longSummary) ? null : longSummary,
            ProseSummary = string.IsNullOrEmpty(proseSummary) ? null : proseSummary,
            Status = new ProcessingStatus(
                Transcribed: true,
                Summarized: true,
                PdfCreated: false,
                Archived: false,
                EmailCreated: false),
        };

        // Step 4 – Persist
        progress?.Report(new("Speichern…", 4, total));
        await _repository.SaveAsync(finalEntry, ct);

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
            Abstract = string.IsNullOrEmpty(abstractText) ? entry.Abstract : abstractText,
            LongSummary = string.IsNullOrEmpty(longSummary) ? entry.LongSummary : longSummary,
            ProseSummary = string.IsNullOrEmpty(proseSummary) ? entry.ProseSummary : proseSummary,
            Status = entry.Status with { Summarized = true },
        };

        // Step 2 – Persist
        progress?.Report(new("Speichern…", 2, total));
        await _repository.SaveAsync(updatedEntry, ct);

        return updatedEntry;
    }

    // -----------------------------------------------------------------------

    private async Task<(string Abstract, string LongSummary, string ProseSummary)>
        GenerateSummariesAsync(string transcript, CancellationToken ct)
    {
        // All three summaries run in parallel for speed
        var abstractTask   = _summaryGenerator.GenerateAbstractAsync(transcript, ct);
        var longTask       = _summaryGenerator.GenerateLongSummaryAsync(transcript, ct);
        var proseTask      = _summaryGenerator.GenerateProseSummaryAsync(transcript, ct);

        await Task.WhenAll(abstractTask, longTask, proseTask);

        return (abstractTask.Result, longTask.Result, proseTask.Result);
    }

    private static string BuildJobId(DateOnly date, int seq)
        => $"{date:yyMMdd}_{seq:D3}_{Guid.NewGuid().ToString("N")[..8]}";
}
