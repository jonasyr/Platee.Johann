namespace Platee.Johann.UiDriver.Audio;

using NAudio.MediaFoundation;
using NAudio.Wave;

/// <summary>
/// Writes a plain-silence MP3 of a given duration, used by the <c>ui-driver silence</c> command
/// to produce oversized fixtures for the 25-MB upload-limit test (D8) without recording anything.
/// </summary>
public static class SilenceMp3
{
    private const int Mp3BitRate = 128_000;

    public static void Write(string path, TimeSpan duration)
    {
        MediaFoundationApi.Startup();

        var format = new WaveFormat(44100, 16, 2);
        var silence = new SilenceProvider(format).ToSampleProvider().Take(duration);
        MediaFoundationEncoder.EncodeToMp3(silence.ToWaveProvider(), path, Mp3BitRate);
    }
}
