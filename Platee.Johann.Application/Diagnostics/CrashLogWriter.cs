namespace Platee.Johann.Application.Diagnostics;

using System.Reflection;

public interface ICrashLogFileSystem
{
    void CreateDirectory(string path);

    void AppendAllText(string path, string contents);
}

public sealed class CrashLogWriter
{
    private const string ProductSegment = "Peano";
    private const string AppSegment = "Johann";
    private const string LogsSegment = "logs";

    private readonly ICrashLogFileSystem fileSystem;
    private readonly Func<DateTimeOffset> utcNow;
    private readonly string appVersion;
    private readonly Lock sync = new();
    private readonly HashSet<string> headerWrittenFiles = [];

    public CrashLogWriter(
        string userProfilePath,
        string? appVersion = null,
        ICrashLogFileSystem? fileSystem = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userProfilePath);

        this.LogDirectory = ResolveLogDirectory(userProfilePath);
        this.appVersion = string.IsNullOrWhiteSpace(appVersion)
            ? Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown"
            : appVersion;
        this.fileSystem = fileSystem ?? new CrashLogFileSystem();
        this.utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public string LogDirectory { get; }

    public static string ResolveLogDirectory(string userProfilePath) =>
        Path.Combine(userProfilePath, ProductSegment, AppSegment, LogsSegment);

    public static string BuildLogFileName(DateOnly date) => $"johann-crash-{date:yyyy-MM-dd}.log";

    public string GetLogFilePath(DateOnly date) => Path.Combine(this.LogDirectory, BuildLogFileName(date));

    public void EnsureLogDirectory() => this.TryCreateDirectory();

    public void WriteCrashLog(string channel, object? ex)
    {
        if (!this.TryCreateDirectory())
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

    private bool TryCreateDirectory()
    {
        try
        {
            this.fileSystem.CreateDirectory(this.LogDirectory);
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
