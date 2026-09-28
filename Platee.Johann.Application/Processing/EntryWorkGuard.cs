namespace Platee.Johann.Application.Processing;

using Platee.Johann.Application.Interfaces;

/// <summary>
/// Keeps deleting and generating apart per entry (#55). Every generation ends by saving the
/// whole entry, so one still running during a deletion would silently bring the entry back.
/// Hence: no deletion while work for the job id is in flight, and no work once it is deleted.
/// Both checks happen under one lock, so there is no gap between "is it busy?" and "mark it".
/// </summary>
internal sealed class EntryWorkGuard
{
    private readonly object gate = new();
    private readonly Dictionary<string, int> active = new(StringComparer.Ordinal);
    private readonly HashSet<string> deleted = new(StringComparer.Ordinal);

    /// <summary>Registers work on the entry until the returned handle is disposed.</summary>
    /// <exception cref="EntryDeletedException">The entry has been deleted.</exception>
    public IDisposable Begin(string jobId)
    {
        lock (this.gate)
        {
            if (this.deleted.Contains(jobId))
            {
                throw new EntryDeletedException();
            }

            this.active[jobId] = this.active.GetValueOrDefault(jobId) + 1;
        }

        return new Release(this, jobId);
    }

    /// <summary>Marks the entry as deleted, unless work on it is still running.</summary>
    /// <exception cref="EntryBusyException">Work on the entry is still running.</exception>
    public void MarkDeleted(string jobId)
    {
        lock (this.gate)
        {
            if (this.active.ContainsKey(jobId))
            {
                throw new EntryBusyException();
            }

            this.deleted.Add(jobId);
        }
    }

    /// <summary>Undoes <see cref="MarkDeleted"/> after a deletion that was rolled back.</summary>
    public void Unmark(string jobId)
    {
        lock (this.gate)
        {
            this.deleted.Remove(jobId);
        }
    }

    private void End(string jobId)
    {
        lock (this.gate)
        {
            var remaining = this.active[jobId] - 1;
            if (remaining == 0)
            {
                this.active.Remove(jobId);
            }
            else
            {
                this.active[jobId] = remaining;
            }
        }
    }

    private sealed class Release(EntryWorkGuard owner, string jobId) : IDisposable
    {
        private int disposed;

        public void Dispose()
        {
            if (Interlocked.Exchange(ref this.disposed, 1) == 0)
            {
                owner.End(jobId);
            }
        }
    }
}
