using Platee.Johann.Application.Interfaces;

namespace Platee.Johann.Infrastructure.Llm;

/// <summary>
/// Stub transcriber — always unavailable (no API key configured).
/// Replaced by WhisperTranscriber when an API key is present.
/// </summary>
public sealed class NoOpAudioTranscriber : IAudioTranscriber
{
    public bool IsAvailable => false;

    public Task<TranscriptionResult> TranscribeAsync(
        string audioFilePath, CancellationToken ct = default)
        => throw new InvalidOperationException(
            "Kein Audio-Transkriptions-Dienst verfügbar. Bitte OPENAI_API_KEY konfigurieren.");
}
