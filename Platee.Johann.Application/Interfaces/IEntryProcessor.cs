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
