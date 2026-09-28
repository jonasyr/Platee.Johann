namespace Platee.Johann.Infrastructure.Llm;

using System.Globalization;

/// <summary>
/// The transcription API takes at most 25 MB per file (#77). A larger recording used to end in
/// a raw SDK error; this checks before anything is uploaded and says what to do instead.
/// <para>
/// For scale: the longest archived dictation is 4.9 MB for 5:17 min, so the limit sits at
/// roughly 25 minutes of MP3. Splitting a recording automatically is deliberately not done
/// until it is actually needed.
/// </para>
/// </summary>
public static class AudioUploadLimit
{
    /// <summary>25 MB as the API counts them (26 214 400 bytes).</summary>
    public const long MaxBytes = 25L * 1024 * 1024;

    /// <exception cref="AudioTooLargeException">The file is larger than <see cref="MaxBytes"/>.</exception>
    public static void Ensure(string audioFilePath)
    {
        var size = new FileInfo(audioFilePath).Length;
        if (size <= MaxBytes)
        {
            return;
        }

        var megabytes = (size / 1024d / 1024d).ToString("0.0", CultureInfo.GetCultureInfo("de-DE"));
        throw new AudioTooLargeException(
            $"Die Aufnahme „{Path.GetFileName(audioFilePath)}“ ist {megabytes} MB groß – die Transkription "
            + "nimmt höchstens 25 MB an (bei MP3 etwa 25 Minuten). Bitte die Aufnahme in kürzere Teile "
            + "aufteilen und diese einzeln einlesen.");
    }
}

/// <summary>A recording too large for the transcription API (#77).</summary>
public sealed class AudioTooLargeException(string message) : InvalidOperationException(message);
