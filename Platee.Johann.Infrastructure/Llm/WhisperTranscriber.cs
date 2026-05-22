namespace Platee.Johann.Infrastructure.Llm;

using OpenAI.Audio;
using Platee.Johann.Application.Interfaces;

/// <summary>
/// OpenAI Whisper transcription provider.
/// Uses whisper-1 with German language setting and Verbose format (provides duration).
/// Mirrors transcribe_audio() from Python transcriber.py.
/// </summary>
public sealed class WhisperTranscriber : IAudioTranscriber
{
    private const string Model = "whisper-1";
    private readonly AudioClient client;

    public bool IsAvailable => true;

    public WhisperTranscriber(string apiKey)
    {
        this.client = new AudioClient(Model, apiKey);
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        string audioFilePath, CancellationToken ct = default)
    {
        var options = new AudioTranscriptionOptions
        {
            Language = "de",
            ResponseFormat = AudioTranscriptionFormat.Verbose,
        };

        await using var stream = File.OpenRead(audioFilePath);
        var response = await this.client.TranscribeAudioAsync(
            stream, Path.GetFileName(audioFilePath), options, ct);

        var transcript = response.Value.Text ?? string.Empty;
        var duration = response.Value.Duration?.TotalSeconds ?? 0.0;
        var wordCount = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return new TranscriptionResult(transcript, duration, wordCount);
    }
}
