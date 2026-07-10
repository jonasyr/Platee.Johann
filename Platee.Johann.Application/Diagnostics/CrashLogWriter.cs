namespace Platee.Johann.Application.Diagnostics;

using System.Reflection;

public interface ICrashLogFileSystem
{
    void CreateDirectory(string path);

    void AppendAllText(string path, string contents);
}

/// <summary>
/// Writes crash and processing-warning entries to a single, unified log file so any failure —
/// crash or non-fatal — is visible on disk without a debugger. Primarily targets
/// <c>C:\Peano\Platee.Johann\logs</c>; if that location cannot be created or written to
/// (e.g. insufficient permissions), it transparently falls back to a per-user writable
/// location so log entries are never silently dropped.
/// </summary>
public sealed class CrashLogWriter
{
    private const string ProductSegment = "Peano";
    private const string AppSegment = "Platee.Johann";
    private const string LogsSegment = "logs";
    private const string DefaultPrimaryRoot = @"C:\";

    private readonly ICrashLogFileSystem fileSystem;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly string appVersion;
    private readonly Lock sync = new();
    private readonly HashSet<string> headerWrittenFiles = [];
    private readonly string primaryDirectory;
    private readonly string fallbackDirectory;

    private string activeDirectory;

    public CrashLogWriter(
        string? primaryRootPath = null,
        string? appVersion = null,
        ICrashLogFileSystem? fileSystem = null,
        Func<DateTimeOffset>? utcNow = null,
        string? fallbackRootPath = null)
    {
        var primaryRoot = string.IsNullOrWhiteSpace(primaryRootPath) ? DefaultPrimaryRoot : primaryRootPath;
        var fallbackRoot = string.IsNullOrWhiteSpace(fallbackRootPath)
            ? Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData)
            : fallbackRootPath;

        this.primaryDirectory = ResolveLogDirectory(primaryRoot);
        this.fallbackDirectory = ResolveLogDirectory(fallbackRoot);
        this.activeDirectory = this.primaryDirectory;

        this.appVersion = string.IsNullOrWhiteSpace(appVersion)
            ? Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown"
            : appVersion;
        this.fileSystem = fileSystem ?? new CrashLogFileSystem();
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    /// <summary>
    /// The directory entries are currently being written to. Starts out as the primary,
    /// unified location and switches to the fallback location if the primary becomes unwritable.
    /// </summary>
    public string LogDirectory => this.activeDirectory;

    public static string ResolveLogDirectory(string rootPath) =>
        Path.Combine(rootPath, ProductSegment, AppSegment, LogsSegment);

    public static string BuildLogFileName(DateOnly date) => $"johann-crash-{date:yyyy-MM-dd}.log";

    public string GetLogFilePath(DateOnly date) => Path.Combine(this.LogDirectory, BuildLogFileName(date));

    public void EnsureLogDirectory() => this.TryEnsureWritableDirectory();

    /// <summary>
    /// Writes a log entry. Used both for unhandled crashes and for non-fatal warnings
    /// (e.g. swallowed processing exceptions) so every failure leaves a diagnostic trail.
    /// </summary>
    public void WriteCrashLog(string channel, object? ex)
    {
        if (!this.TryEnsureWritableDirectory())
        {
            return;
        }

        var now = this.utcNow();
        var date = DateOnly.FromDateTime(now.UtcDateTime);
        var logFilePath = this.GetLogFilePath(date);
        var message = $"[{now:O}] {channel}: {ex}{Environment.NewLine}{Environment.NewLine}";
        var header = $"--- Johann Crash Log | Version: {this.appVersion} | UTC: {now:O} ---{Environment.NewLine}";

        lock (this.sync)
        {
            if (this.headerWrittenFiles.Contains(logFilePath))
            {
                this.TryAppend(logFilePath, message);
                return;
            }

            if (this.TryAppend(logFilePath, header + message))
            {
                this.headerWrittenFiles.Add(logFilePath);
            }
        }
    }

    /// <summary>
    /// Ensures <see cref="activeDirectory"/> points at a directory that can currently be created.
    /// Tries the unified primary location first; falls back to a per-user writable location so
    /// entries are never silently lost when the primary location is inaccessible.
    /// </summary>
    private bool TryEnsureWritableDirectory()
    {
        if (this.TryCreateDirectory(this.primaryDirectory))
        {
            this.activeDirectory = this.primaryDirectory;
            return true;
        }

        if (this.TryCreateDirectory(this.fallbackDirectory))
        {
            this.activeDirectory = this.fallbackDirectory;
            return true;
        }

        return false;
    }

    private bool TryCreateDirectory(string directory)
    {
        try
        {
            this.fileSystem.CreateDirectory(directory);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }

    private bool TryAppend(string logFilePath, string content)
    {
        try
        {
            this.fileSystem.AppendAllText(logFilePath, content);
            return true;
        }
        catch (IOException)
        {
            return false;
        }
        catch (UnauthorizedAccessException)
        {
            return false;
        }
    }
}

internal sealed class CrashLogFileSystem : ICrashLogFileSystem
{
    public void CreateDirectory(string path) => Directory.CreateDirectory(path);

    public void AppendAllText(string path, string contents) => File.AppendAllText(path, contents);
}
