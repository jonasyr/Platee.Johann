namespace Platee.Johann.Application.Diagnostics;

using System.Diagnostics;
using Platee.Johann.Application.Interfaces;

/// <summary>
/// Default <see cref="IEntryProcessingLogger"/> — writes via <see cref="Trace"/> so failures
/// are visible to any attached trace listener without requiring a debugger.
/// </summary>
public sealed class TraceEntryProcessingLogger : IEntryProcessingLogger
{
    public void LogWarning(string operation, string jobId, Exception exception) =>
        Trace.TraceWarning(
            "{0} failed for JobId={1}: {2}: {3}{4}{5}",
            operation,
            jobId,
            exception.GetType().Name,
            exception.Message,
            Environment.NewLine,
            exception.StackTrace);
}
