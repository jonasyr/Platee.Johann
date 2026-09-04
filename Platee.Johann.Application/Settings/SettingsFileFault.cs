namespace Platee.Johann.Application.Settings;

/// <summary>
/// Describes a settings file that could not be read. Reported instead of being
/// swallowed so a corrupt file is never silently replaced by defaults (#45 H2/H3).
/// </summary>
/// <param name="FilePath">The file that failed to load.</param>
/// <param name="BackupPath">
/// Where the unreadable content was copied to, or <c>null</c> if the backup itself failed.
/// </param>
/// <param name="Reason">The underlying error message.</param>
public sealed record SettingsFileFault(string FilePath, string? BackupPath, string Reason);
