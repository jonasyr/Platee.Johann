namespace Platee.Johann.Application.Settings;

/// <summary>
/// Copies an unreadable settings file aside so the next successful save cannot
/// destroy it. A copy — never a move — because the file may live on a share that
/// other clients are reading concurrently.
/// </summary>
public static class CorruptSettingsBackup
{
    public static SettingsFileFault Preserve(string filePath, Exception error, Func<DateTimeOffset>? utcNow = null)
    {
        var stamp = (utcNow?.Invoke() ?? DateTimeOffset.UtcNow).ToString("yyyyMMdd-HHmmssfff");
        var directory = Path.GetDirectoryName(filePath) ?? string.Empty;
        var name = Path.GetFileNameWithoutExtension(filePath);
        var backupPath = Path.Combine(directory, $"{name}.corrupt-{stamp}.json");

        try
        {
            File.Copy(filePath, backupPath, overwrite: true);
            return new SettingsFileFault(filePath, backupPath, error.Message);
        }
        catch (Exception backupError)
        {
            return new SettingsFileFault(
                filePath,
                null,
                $"{error.Message} (Sicherungskopie fehlgeschlagen: {backupError.Message})");
        }
    }
}
