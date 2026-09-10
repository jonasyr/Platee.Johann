using NAudio.Wave;

namespace Platee.Johann.Infrastructure.Audio;

/// <summary>
/// Reads the playing time of an audio file from the file itself.
/// <para>
/// Until v1.4.0 the duration came back from the transcription API: <c>whisper-1</c>
/// reported it in its Verbose response. The current models answer with plain <c>json</c>,
/// which has no duration field, so the value has to be measured locally. NAudio is already
/// a dependency of this project for microphone recording.
/// </para>
/// </summary>
public static class AudioDurationReader
{
    /// <summary>
    /// Returns the playing time in seconds, or <c>0</c> when the file cannot be read.
    /// <para>
    /// Never throws. A duration is a nice-to-have shown next to an entry and in the PDF
    /// header — losing it must never cost the user a dictation that was otherwise
    /// transcribed successfully.
    /// </para>
    /// </summary>
    public static double ReadSeconds(string? audioFilePath)
    {
        if (string.IsNullOrWhiteSpace(audioFilePath) || !File.Exists(audioFilePath))
        {
            return 0.0;
        }

        try
        {
            using var reader = new Mp3FileReader(audioFilePath);
            return reader.TotalTime.TotalSeconds;
        }
        catch (Exception)
        {
            // Not an MP3, truncated, or written by an encoder NAudio cannot parse.
            return TryMediaFoundation(audioFilePath);
        }
    }

    /// <summary>
    /// Second attempt through Media Foundation, which copes with containers and codecs
    /// <see cref="Mp3FileReader"/> rejects. Watch-folder files come from whatever recorder
    /// the user has on their phone, so the format is not guaranteed.
    /// </summary>
    private static double TryMediaFoundation(string audioFilePath)
    {
        try
        {
            using var reader = new MediaFoundationReader(audioFilePath);
            return reader.TotalTime.TotalSeconds;
        }
        catch (Exception)
        {
            return 0.0;
        }
    }
}
