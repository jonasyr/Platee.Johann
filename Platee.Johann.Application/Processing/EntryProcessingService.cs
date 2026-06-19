namespace Platee.Johann.Application.Processing;

using System.IO;
using System.Text;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Parsing;
using Platee.Johann.Domain.Services;
using Platee.Johann.Domain.ValueObjects;

/// <summary>
/// Orchestrates the full pipeline: transcribe → parse → summarize → save → archive.
/// Mirrors process_single_job / process_text_job from Python main.py.
/// </summary>
public sealed class EntryProcessingService : IEntryProcessor
{
    private readonly IAudioTranscriber transcriber;
    private readonly SummaryGenerator summaryGenerator;
    private readonly HeaderParser parser;
    private readonly IEntryRepository repository;
    private readonly string outputRoot;
    private readonly IHtmlOverviewService? overviewService;
    private readonly SettingsHolder settings;
    private readonly IEnumerable<IEntryRenderer> renderers;

    public bool CanProcess => this.transcriber.IsAvailable;

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
        this.transcriber = transcriber;
        this.summaryGenerator = summaryGenerator;
        this.parser = parser;
        this.repository = repository;
        this.outputRoot = outputRoot;
        this.overviewService = overviewService;
        this.settings = settings ?? new SettingsHolder(AppSettings.Default);
        this.renderers = renderers ?? Array.Empty<IEntryRenderer>();
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
        progress?.Report(new("Audio wird transkribiert…", 1, total));
        var transcription = await this.transcriber.TranscribeAsync(audioFilePath, ct);

        var scopedGenerator = this.summaryGenerator.WithSnapshot();
        var settingsSnapshot = this.settings.Current;

        // Step 2 – Header parsing + sequence number
        progress?.Report(new("Metadaten werden analysiert…", 2, total));
        var header = this.parser.Parse(transcription.Transcript);

        int seq = await this.repository.GetNextSequenceNumberAsync(date, ct);

        // Use RemainderText (transcript with type/project tokens stripped) so the
        // title doesn't start with "Aufgabe Johann …" but with the actual content.
        var title = header.ExplicitTitle;

        if (string.IsNullOrWhiteSpace(title) && scopedGenerator.IsAvailable)
        {
            progress?.Report(new("Titel wird generiert…", 2, total));
            title = await scopedGenerator.GenerateTitleAsync(header.RemainderText, ct);
        }

        if (string.IsNullOrWhiteSpace(title))
        {
            title = string.Join(
                " ",
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
        progress?.Report(new("KI erstellt alle Abschnitte…", 3, total));
        var (abstractText, longSummary, proseSummary, taskList, conversationNote, stundenzettelText, analogText, emailText) =
            await this.GenerateSummariesAsync(transcription.Transcript, scopedGenerator, ct);

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
        progress?.Report(new("HTML und PDF werden erstellt…", 4, total));

        var pdfCreated = false;
        var dateFolder = Path.Combine(this.outputRoot, date.ToString("yyyy-MM-dd"));
        var rawFolder = Path.Combine(dateFolder, "_raw");

        Directory.CreateDirectory(dateFolder);
        Directory.CreateDirectory(rawFolder);

        foreach (var renderer in this.renderers)
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
        progress?.Report(new("Eintrag wird gespeichert…", 5, total));
        await this.repository.SaveAsync(finalEntry, ct);
        await this.ArchiveRawFilesAsync(audioFilePath, finalEntry, ct);

        // Move MP3 to configured archive
        var archiveDir = settingsSnapshot.Archivverzeichnis;
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
                await this.repository.SaveAsync(finalEntry, ct);
            }
            catch
            {
                // Ignore move failure
            }
        }

        if (this.overviewService is not null)
        {
            await this.overviewService.RegenerateAsync(date, ct);
        }

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
        if (string.IsNullOrWhiteSpace(entry.EffectiveTranscript))
        {
            throw new InvalidOperationException(
                "Kein Transkript vorhanden – kann nicht neu verarbeiten.");
        }

        var scopedGenerator = this.summaryGenerator.WithSnapshot();

        const int total = 2;

        // Step 1 – Summaries
        progress?.Report(new("Alle Abschnitte werden neu generiert…", 1, total));
        var (abstractText, longSummary, proseSummary, taskList, conversationNote, stundenzettelText, analogText, emailText) =
            await this.GenerateSummariesAsync(entry.EffectiveTranscript!, scopedGenerator, ct);

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
        progress?.Report(new("Aktualisierung wird gespeichert…", 2, total));
        await this.repository.SaveAsync(updatedEntry, ct);

        if (this.overviewService is not null)
        {
            var date = DateOnly.FromDateTime(updatedEntry.CreatedAt.DateTime);
            await this.overviewService.RegenerateAsync(date, ct);
        }

        return updatedEntry;
    }

    public async Task<Entry> ReprocessSectionAsync(Entry entry, string sectionName,
        IProgress<ProcessingProgress>? progress = null, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(entry.EffectiveTranscript))
        {
            throw new InvalidOperationException("Kein Transkript vorhanden.");
        }

        var scopedGenerator = this.summaryGenerator.WithSnapshot();

        progress?.Report(new($"'{sectionName}' wird neu generiert…", 1, 1));

        var updated = sectionName switch
        {
            "Zusammenfassung" => entry with
            {
                LongSummary = await scopedGenerator.GenerateLongSummaryAsync(entry.EffectiveTranscript!, ct),
            },
            "Ausführliche Zusammenfassung" => entry with
            {
                ProseSummary = await scopedGenerator.GenerateProseSummaryAsync(entry.EffectiveTranscript!, ct),
            },
            "Aufgaben" => entry with
            {
                TaskList = await scopedGenerator.GenerateAufgabeAsync(entry.EffectiveTranscript!, ct),
            },
            "Gesprächsnotiz" => entry with
            {
                ConversationNote = await scopedGenerator.GenerateGespraechsnotizAsync(entry.EffectiveTranscript!, ct),
            },
            "E-Mail" => entry with
            {
                EmailText = await scopedGenerator.GenerateEmailTextAsync(
                    entry.ProseSummary ?? entry.LongSummary ?? entry.EffectiveTranscript!, ct),
            },
            "Stundenzettel" => entry with
            {
                StundenzettelText = await scopedGenerator.GenerateStundenzettelAsync(entry.EffectiveTranscript!, ct),
            },
            "Analog" => entry with
            {
                AnalogText = await scopedGenerator.GenerateAnalogAsync(entry.EffectiveTranscript!, ct),
            },
            _ => throw new ArgumentException($"Unbekannte Sektion: {sectionName}"),
        };

        await this.repository.SaveAsync(updated, ct);
        return updated;
    }

    /// <summary>
    /// Re-generates all summaries from a user-corrected transcript,
    /// stores the edited transcript, and persists the updated entry.
    /// </summary>
    public async Task<Entry> RegenerateFromTranscriptAsync(
        Entry entry,
        string editedTranscript,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(editedTranscript))
        {
            throw new InvalidOperationException(
                "Bearbeitetes Transkript darf nicht leer sein.");
        }

        var scopedGenerator = this.summaryGenerator.WithSnapshot();

        const int total = 2;

        // Step 1 – Re-generate all summaries from the edited transcript
        progress?.Report(new("Alle Abschnitte werden aus bearbeitetem Transkript neu generiert…", 1, total));
        var (abstractText, longSummary, proseSummary, taskList, conversationNote, stundenzettelText, analogText, emailText) =
            await this.GenerateSummariesAsync(editedTranscript, scopedGenerator, ct);

        var updatedEntry = entry with
        {
            EditedTranscript = editedTranscript,
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
        progress?.Report(new("Aktualisierung wird gespeichert…", 2, total));
        await this.repository.SaveAsync(updatedEntry, ct);

        if (this.overviewService is not null)
        {
            var date = DateOnly.FromDateTime(updatedEntry.CreatedAt.DateTime);
            await this.overviewService.RegenerateAsync(date, ct);
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
        var scopedGenerator = this.summaryGenerator.WithSnapshot();

        // Prefer ProseSummary, then LongSummary, then Abstract as GPT input
        var source = entry.ProseSummary
            ?? entry.LongSummary
            ?? entry.Abstract
            ?? string.Empty;

        if (scopedGenerator.IsAvailable && !string.IsNullOrWhiteSpace(source))
        {
            return await scopedGenerator.GenerateEmailTextAsync(source, ct);
        }

        // Fallback: compose from available content without GPT
        return BuildFallbackEmailText(entry);
    }

    // ── Private helpers ───────────────────────────────────────────────────────
    private async Task<(string Abstract, string LongSummary, string ProseSummary, string? TaskList, string? ConversationNote, string? StundenzettelText, string? AnalogText, string? EmailText)>
        GenerateSummariesAsync(string transcript, SummaryGenerator scopedGenerator, CancellationToken ct)
    {
        // Step 1: run the three core summaries in parallel
        var abstractTask = scopedGenerator.GenerateAbstractAsync(transcript, ct);
        var longTask = scopedGenerator.GenerateLongSummaryAsync(transcript, ct);
        var proseTask = scopedGenerator.GenerateProseSummaryAsync(transcript, ct);

        await Task.WhenAll(abstractTask, longTask, proseTask);

        var abstractText = abstractTask.Result;
        var longSummary = longTask.Result;
        var proseSummary = proseTask.Result;

        // Step 2: run type-specific summaries in parallel (EmailText depends on proseSummary)
        var taskListTask = scopedGenerator.GenerateAufgabeAsync(transcript, ct);
        var conversationNoteTask = scopedGenerator.GenerateGespraechsnotizAsync(transcript, ct);
        var stundenzettelTask = scopedGenerator.GenerateStundenzettelAsync(transcript, ct);
        var analogTask = scopedGenerator.GenerateAnalogAsync(transcript, ct);
        var emailTask = scopedGenerator.GenerateEmailTextAsync(proseSummary, ct);

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
        if (string.IsNullOrEmpty(this.outputRoot))
        {
            return;
        }

        try
        {
            var date = DateOnly.FromDateTime(entry.CreatedAt.DateTime);
            var rawDir = Path.Combine(this.outputRoot, date.ToString("yyyy-MM-dd"), "_raw");
            Directory.CreateDirectory(rawDir);

            var baseName = FilenameBuilder.Build(entry);

            var audioExt = Path.GetExtension(sourceAudioPath);
            var audioDest = Path.Combine(rawDir, baseName + audioExt);
            if (!File.Exists(audioDest))
            {
                File.Copy(sourceAudioPath, audioDest);
            }

            var effectiveTranscript = entry.EffectiveTranscript;
            if (!string.IsNullOrEmpty(effectiveTranscript))
            {
                var txtPath = Path.Combine(rawDir, baseName + ".txt");
                await File.WriteAllTextAsync(txtPath, effectiveTranscript, ct);
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
        {
            sb.AppendLine(entry.ProseSummary);
        }
        else if (!string.IsNullOrWhiteSpace(entry.Abstract))
        {
            sb.AppendLine(entry.Abstract);
        }

        sb.AppendLine();
        sb.AppendLine($"[{entry.CreatedAt:dd.MM.yyyy} · {entry.ProjectName}]");
        return sb.ToString();
    }

    private static string BuildJobId(DateOnly date, int seq)
        => $"{date:yyMMdd}_{seq:D3}_{Guid.NewGuid().ToString("N")[..8]}";
}
