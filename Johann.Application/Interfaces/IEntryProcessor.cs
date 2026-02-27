using Johann.Domain.Entities;

namespace Johann.Application.Interfaces;

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
}
