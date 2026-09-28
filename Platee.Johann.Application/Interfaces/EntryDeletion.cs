namespace Platee.Johann.Application.Interfaces;

/// <summary>
/// Outcome of <see cref="IEntryRepository.DeleteAsync"/> (#55).
/// </summary>
/// <param name="Found">False when no entry with that job id exists (e.g. removed in Explorer).</param>
/// <param name="TrashDirectory">Folder in the Johann trash that now holds the entry's files.</param>
/// <param name="MovedFiles">Original paths of every file that was moved.</param>
public sealed record EntryDeletionResult(bool Found, string? TrashDirectory, IReadOnlyList<string> MovedFiles)
{
    public static readonly EntryDeletionResult NotFound = new(false, null, []);
}

/// <summary>
/// Deleting an entry failed and was rolled back: every file is where it was before.
/// The message is written for the user and names the file that blocked the deletion.
/// </summary>
public sealed class EntryDeletionException : IOException
{
    public EntryDeletionException(string message, string? blockingFile = null, Exception? innerException = null)
        : base(message, innerException)
    {
        this.BlockingFile = blockingFile;
    }

    public string? BlockingFile { get; }
}

/// <summary>
/// The entry cannot be deleted right now: a generation for it is still running, and its
/// save at the end would bring the entry straight back.
/// </summary>
public sealed class EntryBusyException : InvalidOperationException
{
    public EntryBusyException()
        : base("Der Eintrag wird gerade bearbeitet. Bitte warten, bis die Generierung fertig ist, und dann erneut löschen.")
    {
    }
}

/// <summary>Work was started on an entry that has been deleted; nothing is saved.</summary>
public sealed class EntryDeletedException : InvalidOperationException
{
    public EntryDeletedException()
        : base("Der Eintrag wurde gelöscht.")
    {
    }
}

/// <summary>How long a deleted entry stays restorable in the Johann trash (#55).</summary>
public static class TrashPolicy
{
    public static readonly TimeSpan Retention = TimeSpan.FromDays(30);
}

/// <summary>Outcome of <see cref="IEntryRepository.PurgeTrashAsync"/>.</summary>
/// <param name="Purged">Number of trash folders removed for good.</param>
/// <param name="Skipped">Folders that could not be removed or read, with the reason.</param>
public sealed record TrashPurgeResult(int Purged, IReadOnlyList<string> Skipped)
{
    public static readonly TrashPurgeResult Empty = new(0, []);
}
