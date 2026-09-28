namespace Platee.Johann.Infrastructure.Json;

using System.Text.Json;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Domain.Entities;

/// <summary>
/// The Johann trash (#55): <c>{OutputRoot}/_Papierkorb/{JobId}/</c>, laid out like the day
/// folder it came from — exports at the top, the rest under <c>_raw/</c> — plus a deletion
/// record, so restoring is moving the contents back.
/// <para>
/// The folder name is no date, and its <c>_raw</c> sits one level deeper than the scans in
/// <see cref="JsonRepository"/> look, so a trashed entry is invisible to every lookup.
/// It lives under the output root on purpose: same volume, so a move is a rename.
/// </para>
/// </summary>
internal sealed class EntryTrash
{
    public const string FolderName = "_Papierkorb";
    public const string RecordFileName = "geloescht.json";

    // ERROR_SHARING_VIOLATION / ERROR_LOCK_VIOLATION as HRESULTs.
    private const int SharingViolation = unchecked((int)0x80070020);
    private const int LockViolation = unchecked((int)0x80070021);

    private static readonly JsonSerializerOptions RecordOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    private readonly string trashRoot;
    private readonly Func<DateTimeOffset> utcNow;

    public EntryTrash(string outputRoot, Func<DateTimeOffset> utcNow)
    {
        this.trashRoot = Path.Combine(outputRoot, FolderName);
        this.utcNow = utcNow;
    }

    /// <summary>
    /// Moves the files into a fresh trash folder. All or nothing: if one file cannot be
    /// moved, every file already moved goes back and the folder is removed again.
    /// </summary>
    /// <param name="files">Files of the entry; the status file must come last.</param>
    public EntryDeletionResult Move(Entry entry, DateOnly date, string rawDir, IReadOnlyList<string> files)
    {
        var createdRoot = !Directory.Exists(this.trashRoot);
        var target = this.CreateUniqueFolder(entry.JobId);
        var moved = new List<TrashedFile>();

        try
        {
            foreach (var source in files)
            {
                var inRaw = string.Equals(
                    Path.GetDirectoryName(source), rawDir, StringComparison.OrdinalIgnoreCase);
                var destinationDir = inRaw ? Path.Combine(target, "_raw") : target;
                Directory.CreateDirectory(destinationDir);
                var destination = Path.Combine(destinationDir, Path.GetFileName(source));

                try
                {
                    File.Move(source, destination);
                }
                catch (Exception ex) when (ex is FileNotFoundException or DirectoryNotFoundException)
                {
                    // Gone since the files were collected (e.g. removed in Explorer):
                    // nothing left to delete, and no reason to abort.
                    continue;
                }

                moved.Add(new TrashedFile(source, destination));
            }

            this.WriteRecord(target, entry, date, moved);
            return new EntryDeletionResult(true, target, moved.Select(f => f.From).ToList());
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            var failedFile = FindFailedFile(files, moved);
            var stuck = RollBack(moved);
            if (stuck.Count == 0)
            {
                // A half-written record would otherwise keep the folder alive and make
                // every later purge report it as unreadable.
                TryDeleteFile(Path.Combine(target, RecordFileName));
                TryDelete(target);
                if (createdRoot)
                {
                    TryDelete(this.trashRoot);
                }
            }

            throw BuildError(ex, failedFile, stuck, target);
        }
    }

    public TrashPurgeResult Purge(DateTimeOffset deletedBefore, CancellationToken ct)
    {
        if (!Directory.Exists(this.trashRoot))
        {
            return TrashPurgeResult.Empty;
        }

        var purged = 0;
        var skipped = new List<string>();
        foreach (var folder in Directory.EnumerateDirectories(this.trashRoot))
        {
            ct.ThrowIfCancellationRequested();
            var recordPath = Path.Combine(folder, RecordFileName);
            if (!File.Exists(recordPath))
            {
                // Not written by Johann — not Johann's to remove.
                continue;
            }

            try
            {
                var record = JsonSerializer.Deserialize<TrashRecord>(File.ReadAllText(recordPath), RecordOptions)
                    ?? throw new JsonException("leer");
                if (record.DeletedAt < deletedBefore)
                {
                    Directory.Delete(folder, recursive: true);
                    purged++;
                }
            }
            catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
            {
                skipped.Add($"{folder}: {ex.Message}");
            }
        }

        return purged == 0 && skipped.Count == 0 ? TrashPurgeResult.Empty : new TrashPurgeResult(purged, skipped);
    }

    private string CreateUniqueFolder(string jobId)
    {
        var name = string.Concat(jobId.Select(c => Path.GetInvalidFileNameChars().Contains(c) ? '_' : c));
        var candidate = Path.Combine(this.trashRoot, name);
        for (var n = 2; Directory.Exists(candidate); n++)
        {
            candidate = Path.Combine(this.trashRoot, $"{name}_{n}");
        }

        Directory.CreateDirectory(candidate);
        return candidate;
    }

    private void WriteRecord(string target, Entry entry, DateOnly date, IReadOnlyList<TrashedFile> moved)
    {
        var record = new TrashRecord(
            entry.JobId,
            this.utcNow(),
            date.ToString("yyyy-MM-dd"),
            entry.SequenceNumber,
            entry.ProjectName,
            entry.Title,
            moved);
        File.WriteAllText(Path.Combine(target, RecordFileName), JsonSerializer.Serialize(record, RecordOptions));
    }

    private static string? FindFailedFile(IReadOnlyList<string> files, IReadOnlyList<TrashedFile> moved)
    {
        var done = moved.Select(m => m.From).ToHashSet(StringComparer.OrdinalIgnoreCase);
        return files.FirstOrDefault(f => !done.Contains(f));
    }

    /// <summary>Moves everything back, newest first; returns what could not be restored.</summary>
    private static List<TrashedFile> RollBack(IReadOnlyList<TrashedFile> moved)
    {
        var stuck = new List<TrashedFile>();
        foreach (var file in moved.Reverse())
        {
            try
            {
                File.Move(file.To, file.From);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
            {
                stuck.Add(file);
            }
        }

        return stuck;
    }

    private static EntryDeletionException BuildError(
        Exception ex, string? failedFile, IReadOnlyList<TrashedFile> stuck, string target)
    {
        var name = failedFile is null ? "Der Löschvermerk" : $"„{Path.GetFileName(failedFile)}“";
        var reason = ex switch
        {
            UnauthorizedAccessException => $"{name} darf nicht verschoben werden (keine Berechtigung).",
            IOException io when io.HResult is SharingViolation or LockViolation =>
                $"{name} ist in einem anderen Programm geöffnet. Bitte dort schließen und erneut löschen.",
            _ => $"{name} konnte nicht verschoben werden: {ex.Message}",
        };

        var outcome = stuck.Count == 0
            ? " Es wurde nichts gelöscht."
            : $" {stuck.Count} Datei(en) konnten nicht zurückgelegt werden und liegen in {target}.";

        return new EntryDeletionException(reason + outcome, failedFile, ex);
    }

    private static void TryDeleteFile(string file)
    {
        try
        {
            File.Delete(file);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // Left behind only if the record itself is locked; TryDelete then keeps the folder.
        }
    }

    /// <summary>Removes a folder only if no file is left anywhere below it.</summary>
    private static void TryDelete(string folder)
    {
        try
        {
            if (Directory.Exists(folder) && !Directory.EnumerateFileSystemEntries(folder, "*", SearchOption.AllDirectories)
                    .Any(p => File.Exists(p)))
            {
                Directory.Delete(folder, recursive: true);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // An empty leftover folder is harmless; the entry itself is intact.
        }
    }

    private sealed record TrashedFile(string From, string To);

    private sealed record TrashRecord(
        string JobId,
        DateTimeOffset DeletedAt,
        string Date,
        int SequenceNumber,
        string ProjectName,
        string Title,
        IReadOnlyList<TrashedFile> Files);
}
