namespace Platee.Johann.Application.Diagnostics;

using Platee.Johann.Application.Interfaces;

/// <summary>
/// <see cref="IEntryProcessingLogger"/> that routes into the existing <see cref="CrashLogWriter"/>
/// log file, so processing warnings are visible in the same place as unhandled exceptions.
/// </summary>
public sealed class CrashLogEntryProcessingLogger : IEntryProcessingLogger
{
    private readonly CrashLogWriter crashLogWriter;

    public CrashLogEntryProcessingLogger(CrashLogWriter crashLogWriter)
    {
        this.crashLogWriter = crashLogWriter;
    }

    public void LogWarning(string operation, string jobId, Exception exception) =>
        this.crashLogWriter.WriteCrashLog(
            "WARNING",
            $"{operation} failed for JobId={jobId}: {exception.GetType().Name}: {exception.Message}{Environment.NewLine}{exception.StackTrace}");
}
