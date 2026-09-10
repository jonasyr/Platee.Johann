namespace Platee.Johann.Infrastructure.Llm;

using OpenAI.Audio;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Infrastructure.Audio;

/// <summary>
/// OpenAI speech-to-text provider.
/// <para>
/// The class name is historical: it ran on <c>whisper-1</c> until v1.4.0 and is referenced
/// by that name throughout the wiring. It now uses <c>gpt-transcribe</c>, detects the
/// spoken language itself, and measures the audio duration locally because the current
/// models no longer report it.
/// </para>
/// </summary>
public sealed class WhisperTranscriber : IAudioTranscriber
{
    /// <summary>
    /// The transcription model. Cheaper than <c>whisper-1</c> (0.0045 vs 0.006 $/min) and
    /// markedly more accurate, especially outside German.
    /// </summary>
    public const string ModelName = ModelNames.Transcription;

    /// <summary>
    /// No language is forced on the API. While this was pinned to <c>"de"</c>, an Arabic or
    /// Ukrainian dictation could not be transcribed at all (#58) — the model was made to
    /// hear German. Left unset, it detects the spoken language itself.
    /// <para>
    /// Exposed so the decision is covered by a test rather than buried in an options object.
    /// </para>
    /// </summary>
    public const string? ForcedLanguage = null;

    private readonly AudioClient client;

    public bool IsAvailable => true;

    public WhisperTranscriber(string apiKey)
    {
        this.client = new AudioClient(ModelName, apiKey);
    }

    public async Task<TranscriptionResult> TranscribeAsync(
        string audioFilePath, CancellationToken ct = default)
    {
        var options = new AudioTranscriptionOptions();
        if (ForcedLanguage is not null)
        {
            options.Language = ForcedLanguage;
        }

        await using var stream = File.OpenRead(audioFilePath);
        var response = await this.client.TranscribeAudioAsync(
            stream, Path.GetFileName(audioFilePath), options, ct);

        var transcript = response.Value.Text ?? string.Empty;

        // The duration no longer comes back from the API: only whisper-1's Verbose format
        // carried it, and this model answers with plain json. Measured from the file
        // instead — see AudioDurationReader.
        var duration = AudioDurationReader.ReadSeconds(audioFilePath);
        var wordCount = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length;

        return new TranscriptionResult(transcript, duration, wordCount);
    }
}
