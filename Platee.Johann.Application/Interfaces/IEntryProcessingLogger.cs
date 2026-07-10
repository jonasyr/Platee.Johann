namespace Platee.Johann.Application.Interfaces;

/// <summary>
/// Minimal logging seam for <see cref="Platee.Johann.Application.Processing.EntryProcessingService"/>
/// so non-critical failures (rendering, archiving) leave a diagnostic trail instead of being swallowed.
/// </summary>
public interface IEntryProcessingLogger
{
    void LogWarning(string operation, string jobId, Exception exception);
}
