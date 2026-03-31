using System.Reflection;

namespace Platee.Johann.Application.Diagnostics;

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

    private readonly ICrashLogFileSystem _fileSystem;
    private readonly Func<DateTimeOffset> _utcNow;
    private readonly string _appVersion;
    private readonly Lock _sync = new();
    private readonly HashSet<string> _headerWrittenFiles = [];

    public CrashLogWriter(
        string userProfilePath,
        string? appVersion = null,
        ICrashLogFileSystem? fileSystem = null,
        Func<DateTimeOffset>? utcNow = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(userProfilePath);

        LogDirectory = ResolveLogDirectory(userProfilePath);
        _appVersion = string.IsNullOrWhiteSpace(appVersion)
            ? Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "unknown"
            : appVersion;
        _fileSystem = fileSystem ?? new CrashLogFileSystem();
        _utcNow = utcNow ?? (() => DateTimeOffset.UtcNow);
    }

    public string LogDirectory { get; }

    public static string ResolveLogDirectory(string userProfilePath) =>
        Path.Combine(userProfilePath, ProductSegment, AppSegment, LogsSegment);

    public static string BuildLogFileName(DateOnly date) => $"johann-crash-{date:yyyy-MM-dd}.log";

    public string GetLogFilePath(DateOnly date) => Path.Combine(LogDirectory, BuildLogFileName(date));

    public void EnsureLogDirectory() => TryCreateDirectory();

    public void WriteCrashLog(string channel, object? ex)
    {
        if (!TryCreateDirectory())
            return;

        var now = _utcNow();
        var date = DateOnly.FromDateTime(now.UtcDateTime);
        var logFilePath = GetLogFilePath(date);
        var message = $"[{now:O}] {channel}: {ex}{Environment.NewLine}{Environment.NewLine}";
        var header = $"--- Johann Crash Log | Version: {_appVersion} | UTC: {now:O} ---{Environment.NewLine}";

        lock (_sync)
        {
            if (_headerWrittenFiles.Contains(logFilePath))
            {
                TryAppend(logFilePath, message);
                return;
            }

            if (TryAppend(logFilePath, header + message))
                _headerWrittenFiles.Add(logFilePath);
        }
    }

    private bool TryCreateDirectory()
    {
        try
        {
            _fileSystem.CreateDirectory(LogDirectory);
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
            _fileSystem.AppendAllText(logFilePath, content);
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
