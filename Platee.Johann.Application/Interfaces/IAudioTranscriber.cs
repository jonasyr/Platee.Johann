namespace Platee.Johann.Application.Interfaces;

public sealed record TranscriptionResult(
    string Transcript,
    double DurationSeconds,
    int WordCount);

public interface IAudioTranscriber
{
    bool IsAvailable { get; }

    Task<TranscriptionResult> TranscribeAsync(
        string audioFilePath,
        CancellationToken ct = default);
}
