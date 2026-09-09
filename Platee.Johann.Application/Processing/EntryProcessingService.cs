namespace Platee.Johann.Application.Processing;

using System.Collections.Concurrent;

using System.IO;
using System.Text;
using Platee.Johann.Application.Diagnostics;
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
    private readonly IEntryProcessingLogger logger;

    /// <summary>
    /// Generations currently in flight, keyed by (job id, section id), so a double click
    /// awaits the first call instead of paying for a second one.
    /// </summary>
    private readonly ConcurrentDictionary<(string JobId, string SectionId), Task<Entry>> inFlightSections = new();

    public bool CanProcess => this.transcriber.IsAvailable;

    public EntryProcessingService(
        IAudioTranscriber transcriber,
        SummaryGenerator summaryGenerator,
        HeaderParser parser,
        IEntryRepository repository,
        string outputRoot = "",
        IHtmlOverviewService? overviewService = null,
        SettingsHolder? settings = null,
        IEnumerable<IEntryRenderer>? renderers = null,
        IEntryProcessingLogger? logger = null)
    {
        this.transcriber = transcriber;
        this.summaryGenerator = summaryGenerator;
        this.parser = parser;
        this.repository = repository;
        this.outputRoot = outputRoot;
        this.overviewService = overviewService;
        this.settings = settings ?? new SettingsHolder(AppSettings.Default);
        this.renderers = renderers ?? Array.Empty<IEntryRenderer>();
        this.logger = logger ?? new TraceEntryProcessingLogger();
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

        // Snapshot settings before any async work so mid-flight changes
        // via the non-modal SettingsView cannot affect this run.
        var scopedGenerator = this.summaryGenerator.WithSnapshot();
        var settingsSnapshot = this.settings.Current;

        // Step 1 – Transcription
        progress?.Report(new("Audio wird transkribiert…", 1, total));
        var transcription = await this.transcriber.TranscribeAsync(audioFilePath, ct);

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
        var catalog = this.BuildCatalog();
        var sections = await this.GenerateSummariesAsync(
            transcription.Transcript, scopedGenerator, catalog, ct);

        var finalEntry = baseEntry with
        {
            Abstract = string.IsNullOrEmpty(sections.Abstract) ? null : sections.Abstract,
            LongSummary = string.IsNullOrEmpty(sections.LongSummary) ? null : sections.LongSummary,
            ProseSummary = string.IsNullOrEmpty(sections.ProseSummary) ? null : sections.ProseSummary,
            TaskList = string.IsNullOrEmpty(sections.TaskList) ? null : sections.TaskList,
            ConversationNote = string.IsNullOrEmpty(sections.ConversationNote) ? null : sections.ConversationNote,
            StundenzettelText = string.IsNullOrEmpty(sections.StundenzettelText) ? null : sections.StundenzettelText,
            AnalogText = string.IsNullOrEmpty(sections.AnalogText) ? null : sections.AnalogText,
            EmailText = string.IsNullOrEmpty(sections.EmailText) ? null : sections.EmailText,
            CustomSections = sections.CustomSections,
            CustomSectionNames = NamesFor(catalog, sections.CustomSections.Keys),
            Status = new ProcessingStatus(
                Transcribed: true,
                Summarized: true,
                PdfCreated: false,
                Archived: false,
                EmailCreated: !string.IsNullOrEmpty(sections.EmailText)),
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
                // Case-insensitive to agree with EntryDetailViewModel, which
                // resolves the same renderers with OrdinalIgnoreCase.
                if (IsRenderer(renderer, "PDF"))
                {
                    await renderer.RenderAsync(finalEntry, new RenderOptions(dateFolder, false, true), ct);
                    pdfCreated = true;
                }
                else if (IsRenderer(renderer, "HTML"))
                {
                    await renderer.RenderAsync(finalEntry, new RenderOptions(rawFolder, false, true), ct);
                }
                else if (!IsRenderer(renderer, "Email"))
                {
                    // Email is deliberately on-demand only. Anything else reaching
                    // here is mis-registered, and used to vanish without a trace.
                    this.logger.LogWarning(
                        $"{renderer.RendererName} render",
                        finalEntry.JobId,
                        new InvalidOperationException(
                            $"Renderer '{renderer.RendererName}' is registered but matches no known output; it was skipped."));
                }
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"{renderer.RendererName} render", finalEntry.JobId, ex);
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
            catch (Exception ex)
            {
                this.logger.LogWarning("MP3 archive move", finalEntry.JobId, ex);
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
        var catalog = this.BuildRegenerationCatalog(entry);
        var sections = await this.GenerateSummariesAsync(
            entry.EffectiveTranscript!, scopedGenerator, catalog, ct);

        var updatedEntry = entry with
        {
            Abstract = string.IsNullOrEmpty(sections.Abstract) ? entry.Abstract : sections.Abstract,
            LongSummary = string.IsNullOrEmpty(sections.LongSummary) ? entry.LongSummary : sections.LongSummary,
            ProseSummary = string.IsNullOrEmpty(sections.ProseSummary) ? entry.ProseSummary : sections.ProseSummary,
            TaskList = string.IsNullOrEmpty(sections.TaskList) ? entry.TaskList : sections.TaskList,
            ConversationNote = string.IsNullOrEmpty(sections.ConversationNote) ? entry.ConversationNote : sections.ConversationNote,
            StundenzettelText = string.IsNullOrEmpty(sections.StundenzettelText) ? entry.StundenzettelText : sections.StundenzettelText,
            AnalogText = string.IsNullOrEmpty(sections.AnalogText) ? entry.AnalogText : sections.AnalogText,
            EmailText = string.IsNullOrEmpty(sections.EmailText) ? entry.EmailText : sections.EmailText,
            CustomSections = MergeCustomSections(entry.CustomSections, sections.CustomSections),
            CustomSectionNames = MergeCustomSectionNames(
                entry.CustomSectionNames, NamesFor(catalog, sections.CustomSections.Keys)),
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
    /// Renderer dispatch matches on name. This is the single comparison rule for
    /// the pipeline, deliberately case-insensitive so it agrees with
    /// EntryDetailViewModel's lookups rather than diverging from them.
    /// </summary>
    private static bool IsRenderer(IEntryRenderer renderer, string name) =>
        renderer.RendererName.Equals(name, StringComparison.OrdinalIgnoreCase);

    /// <summary>
    /// Generates exactly one section by its stable id, merges it into the entry, and saves.
    /// <para>
    /// Concurrent calls for the same (entry, section) pair share a single LLM call: a
    /// CanExecute flag in the UI cannot prevent two clicks being dispatched before the first
    /// command completes, and every wasted call costs real money.
    /// </para>
    /// </summary>
    public Task<Entry> GenerateSectionAsync(
        Entry entry,
        string sectionId,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default)
    {
        var key = (entry.JobId, sectionId);

        // GetOrAdd's factory may run more than once under contention, but only one task is
        // ever stored and returned, so both callers await the same generation.
        var task = this.inFlightSections.GetOrAdd(
            key, _ => this.RunSectionAsync(entry, sectionId, progress, ct));

        return AwaitAndReleaseAsync(task, key);

        async Task<Entry> AwaitAndReleaseAsync(Task<Entry> pending, (string, string) cacheKey)
        {
            try
            {
                return await pending.ConfigureAwait(false);
            }
            finally
            {
                // Released on failure too, so a transient LLM error does not permanently
                // wedge the section behind a cached faulted task.
                this.inFlightSections.TryRemove(cacheKey, out _);
            }
        }
    }

    private async Task<Entry> RunSectionAsync(
        Entry entry, string sectionId, IProgress<ProcessingProgress>? progress, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(entry.EffectiveTranscript))
        {
            throw new InvalidOperationException("Kein Transkript vorhanden.");
        }

        var descriptor = this.BuildCatalog().FirstOrDefault(s => s.Id == sectionId)
            ?? throw new ArgumentException($"Unbekannte Sektion: {sectionId}", nameof(sectionId));

        var generator = this.summaryGenerator.WithSnapshot();
        var transcript = entry.EffectiveTranscript!;

        progress?.Report(new($"'{descriptor.Name}' wird generiert…", 1, 1));

        var updated = sectionId switch
        {
            BuiltInSections.LongSummary => entry with
            {
                LongSummary = await generator.GenerateLongSummaryAsync(transcript, ct),
            },
            BuiltInSections.ProseSummary => entry with
            {
                ProseSummary = await generator.GenerateProseSummaryAsync(transcript, ct),
            },
            BuiltInSections.TaskList => entry with
            {
                TaskList = await generator.GenerateAufgabeAsync(transcript, ct),
            },
            BuiltInSections.ConversationNote => entry with
            {
                ConversationNote = await generator.GenerateGespraechsnotizAsync(transcript, ct),
            },
            BuiltInSections.Stundenzettel => entry with
            {
                StundenzettelText = await generator.GenerateStundenzettelAsync(transcript, ct),
            },
            BuiltInSections.Analog => entry with
            {
                AnalogText = await generator.GenerateAnalogAsync(transcript, ct),
            },
            BuiltInSections.EmailText => entry with
            {
                // EmailText reads a summary rather than the transcript; keep the original
                // fallback chain so an entry without a prose summary still produces one.
                EmailText = await generator.GenerateEmailTextAsync(
                    entry.ProseSummary ?? entry.LongSummary ?? transcript, ct),
            },
            _ => await GenerateCustomAsync(entry, descriptor, generator, transcript, ct),
        };

        await this.repository.SaveAsync(updated, ct);

        // The daily overview is a rendered artefact of the entries, so every path that
        // persists one has to refresh it — otherwise a section generated on demand is
        // missing from _ItemÜbersicht.html until the next full run.
        if (this.overviewService is not null)
        {
            await this.overviewService.RegenerateAsync(
                DateOnly.FromDateTime(updated.CreatedAt.DateTime), ct);
        }

        return updated;

        static async Task<Entry> GenerateCustomAsync(
            Entry entry, SectionDescriptor descriptor, SummaryGenerator generator,
            string transcript, CancellationToken ct)
        {
            var text = await generator.GenerateCustomSectionAsync(descriptor.Category!, transcript, ct);
            var sections = new Dictionary<string, string>(entry.CustomSections, StringComparer.Ordinal);

            if (string.IsNullOrWhiteSpace(text))
            {
                sections.Remove(descriptor.Id);
            }
            else
            {
                sections[descriptor.Id] = text;
            }

            var names = new Dictionary<string, string>(entry.CustomSectionNames, StringComparer.Ordinal);
            if (string.IsNullOrWhiteSpace(text))
            {
                names.Remove(descriptor.Id);
            }
            else
            {
                names[descriptor.Id] = descriptor.Name;
            }

            return entry with { CustomSections = sections, CustomSectionNames = names };
        }
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
        var catalog = this.BuildRegenerationCatalog(entry);
        var sections = await this.GenerateSummariesAsync(
            editedTranscript, scopedGenerator, catalog, ct);

        var updatedEntry = entry with
        {
            EditedTranscript = editedTranscript,
            Abstract = string.IsNullOrEmpty(sections.Abstract) ? entry.Abstract : sections.Abstract,
            LongSummary = string.IsNullOrEmpty(sections.LongSummary) ? entry.LongSummary : sections.LongSummary,
            ProseSummary = string.IsNullOrEmpty(sections.ProseSummary) ? entry.ProseSummary : sections.ProseSummary,
            TaskList = string.IsNullOrEmpty(sections.TaskList) ? entry.TaskList : sections.TaskList,
            ConversationNote = string.IsNullOrEmpty(sections.ConversationNote) ? entry.ConversationNote : sections.ConversationNote,
            StundenzettelText = string.IsNullOrEmpty(sections.StundenzettelText) ? entry.StundenzettelText : sections.StundenzettelText,
            AnalogText = string.IsNullOrEmpty(sections.AnalogText) ? entry.AnalogText : sections.AnalogText,
            EmailText = string.IsNullOrEmpty(sections.EmailText) ? entry.EmailText : sections.EmailText,
            CustomSections = MergeCustomSections(entry.CustomSections, sections.CustomSections),
            CustomSectionNames = MergeCustomSectionNames(
                entry.CustomSectionNames, NamesFor(catalog, sections.CustomSections.Keys)),
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

    /// <summary>
    /// The output of one generation run. Replaces the former eight-element tuple, which
    /// could not carry the user-defined sections.
    /// </summary>
    private sealed record GeneratedSections(
        string Abstract,
        string LongSummary,
        string ProseSummary,
        string? TaskList,
        string? ConversationNote,
        string? StundenzettelText,
        string? AnalogText,
        string? EmailText,
        IReadOnlyDictionary<string, string> CustomSections);

    // ── Private helpers ───────────────────────────────────────────────────────
    /// <summary>
    /// Runs every section whose mode is <see cref="GenerationMode.Auto"/>, in parallel.
    /// <para>
    /// Abstract and Title are intrinsics: they drive the entry list, so they always run and
    /// are never part of the catalog. EmailText is derived from ProseSummary rather than the
    /// transcript, so it stays chained after it instead of running alongside.
    /// </para>
    /// </summary>
    private async Task<GeneratedSections> GenerateSummariesAsync(
        string transcript,
        SummaryGenerator scopedGenerator,
        IReadOnlyList<SectionDescriptor> catalog,
        CancellationToken ct)
    {
        var auto = new HashSet<string>(
            catalog.Where(s => s.Mode == GenerationMode.Auto).Select(s => s.Id),
            StringComparer.Ordinal);

        // Step 1: the intrinsic abstract plus the two core summaries, in parallel.
        var abstractTask = scopedGenerator.GenerateAbstractAsync(transcript, ct);
        var longTask = auto.Contains(BuiltInSections.LongSummary)
            ? scopedGenerator.GenerateLongSummaryAsync(transcript, ct)
            : Task.FromResult(string.Empty);
        var proseTask = auto.Contains(BuiltInSections.ProseSummary)
            ? scopedGenerator.GenerateProseSummaryAsync(transcript, ct)
            : Task.FromResult(string.Empty);

        await Task.WhenAll(abstractTask, longTask, proseTask);

        var proseSummary = await proseTask;

        // Step 2: the remaining built-ins plus every auto custom category, in parallel.
        var taskListTask = auto.Contains(BuiltInSections.TaskList)
            ? scopedGenerator.GenerateAufgabeAsync(transcript, ct)
            : Task.FromResult<string?>(null);
        var conversationNoteTask = auto.Contains(BuiltInSections.ConversationNote)
            ? scopedGenerator.GenerateGespraechsnotizAsync(transcript, ct)
            : Task.FromResult<string?>(null);
        var stundenzettelTask = auto.Contains(BuiltInSections.Stundenzettel)
            ? scopedGenerator.GenerateStundenzettelAsync(transcript, ct)
            : Task.FromResult<string?>(null);
        var analogTask = auto.Contains(BuiltInSections.Analog)
            ? scopedGenerator.GenerateAnalogAsync(transcript, ct)
            : Task.FromResult<string?>(null);

        // EmailText reads the prose summary, so an auto E-Mail with an on-demand prose
        // summary would have nothing to work from — fall back to the transcript.
        var emailTask = auto.Contains(BuiltInSections.EmailText)
            ? scopedGenerator.GenerateEmailTextAsync(
                string.IsNullOrWhiteSpace(proseSummary) ? transcript : proseSummary, ct)
            : Task.FromResult<string?>(null);

        var customTasks = catalog
            .Where(s => !s.IsBuiltIn && s.Category is not null && auto.Contains(s.Id))
            .ToDictionary(
                s => s.Id,
                s => scopedGenerator.GenerateCustomSectionAsync(s.Category!, transcript, ct),
                StringComparer.Ordinal);

        await Task.WhenAll(
            [
                (Task)taskListTask, conversationNoteTask, stundenzettelTask,
                analogTask, emailTask, .. customTasks.Values,
            ]);

        var customSections = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var (id, task) in customTasks)
        {
            var text = await task;
            if (!string.IsNullOrWhiteSpace(text))
            {
                customSections[id] = text;
            }
        }

        return new GeneratedSections(
            await abstractTask,
            await longTask,
            proseSummary,
            await taskListTask,
            await conversationNoteTask,
            await stundenzettelTask,
            await analogTask,
            await emailTask,
            customSections);
    }

    /// <summary>
    /// Builds the section catalog from the settings currently in force.
    /// </summary>
    private IReadOnlyList<SectionDescriptor> BuildCatalog() =>
        SectionCatalog.Build(this.settings.Prompts, this.settings.Current.SectionModes);

    /// <summary>
    /// The catalog used when regenerating an existing entry.
    /// <para>
    /// Sections that already carry text are promoted to <see cref="GenerationMode.Auto"/>,
    /// so a regeneration refreshes what the user can actually see. On-demand sections that
    /// were never generated stay untouched — regenerating a transcript must not silently
    /// spend GPT calls on sections the user deliberately left unproduced.
    /// </para>
    /// </summary>
    private IReadOnlyList<SectionDescriptor> BuildRegenerationCatalog(Entry entry) =>
        [.. this.BuildCatalog().Select(s =>
            s.Mode == GenerationMode.Auto || !HasContent(entry, s.Id)
                ? s
                : s with { Mode = GenerationMode.Auto })];

    private static bool HasContent(Entry entry, string sectionId) => sectionId switch
    {
        BuiltInSections.LongSummary => !string.IsNullOrWhiteSpace(entry.LongSummary),
        BuiltInSections.ProseSummary => !string.IsNullOrWhiteSpace(entry.ProseSummary),
        BuiltInSections.TaskList => !string.IsNullOrWhiteSpace(entry.TaskList),
        BuiltInSections.ConversationNote => !string.IsNullOrWhiteSpace(entry.ConversationNote),
        BuiltInSections.EmailText => !string.IsNullOrWhiteSpace(entry.EmailText),
        BuiltInSections.Stundenzettel => !string.IsNullOrWhiteSpace(entry.StundenzettelText),
        BuiltInSections.Analog => !string.IsNullOrWhiteSpace(entry.AnalogText),
        _ => entry.CustomSections.TryGetValue(sectionId, out var text)
             && !string.IsNullOrWhiteSpace(text),
    };

    /// <summary>
    /// Overlays freshly generated custom sections onto the existing ones. Sections that were
    /// not regenerated keep their previous text rather than disappearing.
    /// </summary>
    /// <summary>
    /// The display name of every generated custom section, taken from the catalog in force at
    /// generation time. Stored on the entry so deleting the category later leaves a readable
    /// heading behind instead of a raw id.
    /// </summary>
    private static IReadOnlyDictionary<string, string> NamesFor(
        IReadOnlyList<SectionDescriptor> catalog, IEnumerable<string> generatedIds)
    {
        var wanted = new HashSet<string>(generatedIds, StringComparer.Ordinal);
        return catalog
            .Where(d => !d.IsBuiltIn && wanted.Contains(d.Id))
            .ToDictionary(d => d.Id, d => d.Name, StringComparer.Ordinal);
    }

    /// <summary>
    /// Overlays freshly recorded section names onto the existing ones, mirroring
    /// <see cref="MergeCustomSections"/> so a name never outlives or precedes its text.
    /// </summary>
    private static IReadOnlyDictionary<string, string> MergeCustomSectionNames(
        IReadOnlyDictionary<string, string> existing,
        IReadOnlyDictionary<string, string> regenerated)
    {
        var merged = new Dictionary<string, string>(existing, StringComparer.Ordinal);
        foreach (var (id, name) in regenerated)
        {
            merged[id] = name;
        }

        return merged;
    }

    private static IReadOnlyDictionary<string, string> MergeCustomSections(
        IReadOnlyDictionary<string, string> existing,
        IReadOnlyDictionary<string, string> regenerated)
    {
        var merged = new Dictionary<string, string>(existing, StringComparer.Ordinal);
        foreach (var (id, text) in regenerated)
        {
            merged[id] = text;
        }

        return merged;
    }

    /// <summary>
    /// Copies the source audio file and writes the transcript text into
    /// {outputRoot}/{YYYY-MM-DD}/_raw/ using the FilenameBuilder convention.
    /// Failures are non-fatal — archival is non-critical — but are logged via <see cref="logger"/>.
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
        catch (Exception ex)
        {
            // Non-critical: archival failure must not break the pipeline, but it is logged.
            this.logger.LogWarning("Raw file archival", entry.JobId, ex);
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
