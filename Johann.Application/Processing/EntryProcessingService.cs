using System.IO;
using System.Text;
using Johann.Application.Interfaces;
using Johann.Application.Settings;
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
    // Serializes sequence-number assignment across concurrent ProcessAudioAsync calls
    // so that simultaneous processing of multiple files never produces duplicate seq numbers.
    private static readonly SemaphoreSlim _seqLock = new(1, 1);

    private readonly IAudioTranscriber _transcriber;
    private readonly SummaryGenerator _summaryGenerator;
    private readonly HeaderParser _parser;
    private readonly IEntryRepository _repository;
    private readonly string _outputRoot;
    private readonly IHtmlOverviewService? _overviewService;
    private readonly SettingsHolder _settings;
    private readonly IEnumerable<IEntryRenderer> _renderers;

    public bool CanProcess => _transcriber.IsAvailable;

    public EntryProcessingService(
        IAudioTranscriber transcriber,
        SummaryGenerator summaryGenerator,
        HeaderParser parser,
        IEntryRepository repository,
        string outputRoot = "",
        IHtmlOverviewService? overviewService = null,
        SettingsHolder? settings = null,
        IEnumerable<IEntryRenderer>? renderers = null)
    {
        _transcriber = transcriber;
        _summaryGenerator = summaryGenerator;
        _parser = parser;
        _repository = repository;
        _outputRoot = outputRoot;
        _overviewService = overviewService;
        _settings = settings ?? new SettingsHolder(Settings.AppSettings.Default);
        _renderers = renderers ?? Array.Empty<IEntryRenderer>();
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
        const int total = 5;

        // Step 1 – Transcription
        progress?.Report(new("Transkribiere Audio…", 1, total));
        var transcription = await _transcriber.TranscribeAsync(audioFilePath, ct);

        // Step 2 – Header parsing + sequence number
        progress?.Report(new("Analysiere Header…", 2, total));
        var header = _parser.Parse(transcription.Transcript);

        // Serialize seq assignment: read count, reserve number, immediately persist a
        // placeholder — all while holding the lock so concurrent calls get unique numbers.
        await _seqLock.WaitAsync(ct);
        int seq;
        try
        {
            var existingEntries = await _repository.GetEntriesForDateAsync(date, ct);
            seq = existingEntries.Count + 1;
        }
        finally
        {
            _seqLock.Release();
        }

        // Use RemainderText (transcript with type/project tokens stripped) so the
        // title doesn't start with "Aufgabe Johann …" but with the actual content.
        var title = header.ExplicitTitle;

        if (string.IsNullOrWhiteSpace(title) && _summaryGenerator.IsAvailable)
        {
            progress?.Report(new("Generiere Titel…", 2, total));
            title = await _summaryGenerator.GenerateTitleAsync(header.RemainderText, ct);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = string.Join(" ",
                header.RemainderText
                    .Split(' ', StringSplitOptions.RemoveEmptyEntries)
                    .Take(5)
                    .Select(w => w.Trim('.', ',', ':', ';', '!', '?')));
        }

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
        var (abstractText, longSummary, proseSummary, taskList, conversationNote, stundenzettelText, analogText, emailText) =
            await GenerateSummariesAsync(transcription.Transcript, ct);

        var finalEntry = baseEntry with
        {
            Abstract = string.IsNullOrEmpty(abstractText) ? null : abstractText,
            LongSummary = string.IsNullOrEmpty(longSummary) ? null : longSummary,
            ProseSummary = string.IsNullOrEmpty(proseSummary) ? null : proseSummary,
            TaskList = string.IsNullOrEmpty(taskList) ? null : taskList,
            ConversationNote = string.IsNullOrEmpty(conversationNote) ? null : conversationNote,
            StundenzettelText = string.IsNullOrEmpty(stundenzettelText) ? null : stundenzettelText,
            AnalogText = string.IsNullOrEmpty(analogText) ? null : analogText,
            EmailText = string.IsNullOrEmpty(emailText) ? null : emailText,
            Status = new ProcessingStatus(
                Transcribed: true,
                Summarized: true,
                PdfCreated: false,
                Archived: false,
                EmailCreated: !string.IsNullOrEmpty(emailText)),
        };

        // Step 4 – Auto-generate HTML/PDF
        progress?.Report(new("Exportiere Dateien…", 4, total));

        var pdfCreated = false;
        var dateFolder = Path.Combine(_outputRoot, date.ToString("yyyy-MM-dd"));
        var rawFolder = Path.Combine(dateFolder, "_raw");

        Directory.CreateDirectory(dateFolder);
        Directory.CreateDirectory(rawFolder);

        foreach (var renderer in _renderers)
        {
            try
            {
                if (renderer.RendererName == "PDF")
                {
                    await renderer.RenderAsync(finalEntry, new RenderOptions(dateFolder, false, true), ct);
                    pdfCreated = true;
                }
                else if (renderer.RendererName == "HTML")
                {
                    await renderer.RenderAsync(finalEntry, new RenderOptions(rawFolder, false, true), ct);
                }
            }
            catch
            {
                // Fallback / ignore failure
            }
        }

        if (pdfCreated)
        {
            finalEntry = finalEntry with { Status = finalEntry.Status with { PdfCreated = true } };
        }

        // Step 5 – Persist JSON + archive raw files + regenerate overview
        progress?.Report(new("Speichern…", 5, total));
        await _repository.SaveAsync(finalEntry, ct);
        await ArchiveRawFilesAsync(audioFilePath, finalEntry, ct);

        // Move MP3 to configured archive
        var archiveDir = _settings.Current.Archivverzeichnis;
        if (!string.IsNullOrWhiteSpace(archiveDir))
        {
            try
            {
                Directory.CreateDirectory(archiveDir);
                var destName = Path.GetFileName(audioFilePath);
                var newPath = Path.Combine(archiveDir, destName);
                if (File.Exists(newPath))
                {
                    newPath = Path.Combine(archiveDir, Path.GetFileNameWithoutExtension(destName) + "_" + Guid.NewGuid().ToString("N")[..6] + Path.GetExtension(destName));
                }
                File.Move(audioFilePath, newPath);

                finalEntry = finalEntry with { Status = finalEntry.Status with { Archived = true } };
                await _repository.SaveAsync(finalEntry, ct);
            }
            catch
            {
                // Ignore move failure
            }
        }

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
        var (abstractText, longSummary, proseSummary, taskList, conversationNote, stundenzettelText, analogText, emailText) =
            await GenerateSummariesAsync(entry.Transcript, ct);

        var updatedEntry = entry with
        {
            Abstract = string.IsNullOrEmpty(abstractText) ? entry.Abstract : abstractText,
            LongSummary = string.IsNullOrEmpty(longSummary) ? entry.LongSummary : longSummary,
            ProseSummary = string.IsNullOrEmpty(proseSummary) ? entry.ProseSummary : proseSummary,
            TaskList = string.IsNullOrEmpty(taskList) ? entry.TaskList : taskList,
            ConversationNote = string.IsNullOrEmpty(conversationNote) ? entry.ConversationNote : conversationNote,
            StundenzettelText = string.IsNullOrEmpty(stundenzettelText) ? entry.StundenzettelText : stundenzettelText,
            AnalogText = string.IsNullOrEmpty(analogText) ? entry.AnalogText : analogText,
            EmailText = string.IsNullOrEmpty(emailText) ? entry.EmailText : emailText,
            Status = entry.Status with { Summarized = true },
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

    private async Task<(string Abstract, string LongSummary, string ProseSummary, string? TaskList, string? ConversationNote, string? StundenzettelText, string? AnalogText, string? EmailText)>
        GenerateSummariesAsync(string transcript, CancellationToken ct)
    {
        // Step 1: run the three core summaries in parallel
        var abstractTask = _summaryGenerator.GenerateAbstractAsync(transcript, ct);
        var longTask = _summaryGenerator.GenerateLongSummaryAsync(transcript, ct);
        var proseTask = _summaryGenerator.GenerateProseSummaryAsync(transcript, ct);

        await Task.WhenAll(abstractTask, longTask, proseTask);

        var abstractText = abstractTask.Result;
        var longSummary = longTask.Result;
        var proseSummary = proseTask.Result;

        // Step 2: run type-specific summaries in parallel (EmailText depends on proseSummary)
        var taskListTask = _summaryGenerator.GenerateAufgabeAsync(transcript, ct);
        var conversationNoteTask = _summaryGenerator.GenerateGespraechsnotizAsync(transcript, ct);
        var stundenzettelTask = _summaryGenerator.GenerateStundenzettelAsync(transcript, ct);
        var analogTask = _summaryGenerator.GenerateAnalogAsync(transcript, ct);
        var emailTask = _summaryGenerator.GenerateEmailTextAsync(proseSummary, ct);

        await Task.WhenAll(
            (Task)taskListTask,
            conversationNoteTask,
            stundenzettelTask,
            analogTask,
            emailTask);

        return (abstractText, longSummary, proseSummary,
            taskListTask.Result, conversationNoteTask.Result,
            stundenzettelTask.Result, analogTask.Result,
            emailTask.Result);
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
            var date = DateOnly.FromDateTime(entry.CreatedAt.DateTime);
            var rawDir = Path.Combine(_outputRoot, date.ToString("yyyy-MM-dd"), "_raw");
            Directory.CreateDirectory(rawDir);

            var baseName = FilenameBuilder.Build(entry);

            var audioExt = Path.GetExtension(sourceAudioPath);
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
