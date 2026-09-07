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

    /// <summary>
    /// Legacy entry point keyed by German display name. Superseded by
    /// <see cref="GenerateSectionAsync"/>; removed once the XAML migrates (#53).
    /// </summary>
    [Obsolete("Use GenerateSectionAsync with a stable section id.")]
    Task<Entry> ReprocessSectionAsync(
        Entry entry,
        string sectionName,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default);

    Task<Entry> RegenerateFromTranscriptAsync(
        Entry entry,
        string editedTranscript,
        IProgress<ProcessingProgress>? progress = null,
        CancellationToken ct = default);
}
