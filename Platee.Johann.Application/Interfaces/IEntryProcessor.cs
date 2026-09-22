namespace Platee.Johann.Application.Interfaces;

using Platee.Johann.Domain.Entities;

public sealed record ProcessingProgress(
    string Stage,
    int StepIndex,
    int TotalSteps,
    string? Detail = null);

public interface IEntryProcessor
{
    bool CanProcess { get; }

    Task<Entry> ProcessAudioAsync(
        string audioFilePath,
        DateOnly date,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default);

    Task<Entry> ReprocessAsync(
        Entry entry,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Generates an email text for the entry via GPT (if available),
    /// or falls back to a simple plain-text composition.
    /// </summary>
    Task<string> GenerateEmailTextAsync(
        Entry entry,
        CancellationToken ct = default);

    /// <summary>
    /// Generates exactly one section by its stable id and persists the result.
    /// Concurrent calls for the same (entry, section) share a single LLM call.
    /// </summary>
    Task<Entry> GenerateSectionAsync(
        Entry entry,
        string sectionId,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default);

    Task<Entry> RegenerateFromTranscriptAsync(
        Entry entry,
        string editedTranscript,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default);

    /// <summary>
    /// Saves the "erledigt" flag. Goes through the processor, not straight to the repository,
    /// so it is covered by the same guard as every other save of an existing entry (#55).
    /// </summary>
    /// <exception cref="EntryDeletedException">The entry has been deleted.</exception>
    Task<Entry> SetDoneAsync(Entry entry, bool isDone, CancellationToken ct = default);

    /// <summary>
    /// Moves the entry into the Johann trash and refreshes the day's overview (#55).
    /// Refused while a generation for the entry is running; afterwards every generation for
    /// it is refused, so a late save can never bring the entry back.
    /// </summary>
    /// <exception cref="EntryBusyException">A generation for the entry is still running.</exception>
    /// <exception cref="EntryDeletionException">A file could not be moved; nothing changed.</exception>
    Task<EntryDeletionResult> DeleteAsync(Entry entry, CancellationToken ct = default);
}
