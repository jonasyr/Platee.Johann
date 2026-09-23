namespace Platee.Johann.UI.Helpers;

/// <summary>
/// Keeps a dictation whose processing failed (#106). Until v1.5.0 the temporary recording was
/// deleted on every failure — no network, a server error, a file over 25 MB — and the
/// dictation was lost. Watch-folder files never had this problem: they stay in the input
/// folder. Nothing re-processes a rescued recording automatically; one too large would only
/// fail again (→ #107).
/// </summary>
public static class DictationRescue
{
    public const string FolderName = "_Diktate (nicht verarbeitet)";

    /// <summary>
    /// Moves <paramref name="tempRecordingPath"/> to <c>{outputRoot}\_Diktate (nicht verarbeitet)</c>.
    /// </summary>
    /// <returns>
    /// Where the recording now is: the rescue folder, or — if it could not be moved there —
    /// still the temp path, which then must not be deleted. <c>null</c> when the recording no
    /// longer exists (processing failed after it was archived).
    /// </returns>
    public static string? Save(string tempRecordingPath, string outputRoot, DateTime now)
    {
        if (!File.Exists(tempRecordingPath))
        {
            return null;
        }

        try
        {
            var folder = Path.Combine(outputRoot, FolderName);
            Directory.CreateDirectory(folder);

            var stem = $"Diktat_{now:yyyy-MM-dd_HHmmss}";
            var extension = Path.GetExtension(tempRecordingPath);
            var target = Path.Combine(folder, stem + extension);
            for (var n = 2; File.Exists(target); n++)
            {
                target = Path.Combine(folder, $"{stem}_{n}{extension}");
            }

            File.Move(tempRecordingPath, target);
            return target;
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or NotSupportedException)
        {
            // Better left in the temp folder and named in the message than lost.
            return tempRecordingPath;
        }
    }
}
