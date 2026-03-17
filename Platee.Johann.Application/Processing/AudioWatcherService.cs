using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;

namespace Platee.Johann.Application.Processing;

/// <summary>
/// Monitors a target directory for new MP3 files and automatically processes them.
/// </summary>
public sealed class AudioWatcherService : IDisposable
{
    private readonly IEntryProcessor _processor;
    private readonly SettingsHolder _settings;
    private FileSystemWatcher? _watcher;

    /// <summary>
    /// Raised on a background thread after an audio file is successfully processed.
    /// Subscribers must marshal to the UI thread themselves.
    /// </summary>
    public event Action<Platee.Johann.Domain.Entities.Entry>? EntryProcessed;

    public AudioWatcherService(IEntryProcessor processor, SettingsHolder settings)
    {
        _processor = processor;
        _settings = settings;
    }

    public void Start()
    {
        var inputPath = _settings.Current.Quellverzeichnis;
        if (string.IsNullOrWhiteSpace(inputPath))
            return;

        Directory.CreateDirectory(inputPath);

        // Process existing files first
        ProcessExistingFiles(inputPath);

        _watcher = new FileSystemWatcher(inputPath, "*.mp3")
        {
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
            EnableRaisingEvents = true
        };

        _watcher.Created += OnFileCreated;
    }

    private void ProcessExistingFiles(string inputPath)
    {
        // Fire and forget so we don't block startup
        Task.Run(async () =>
        {
            var files = Directory.GetFiles(inputPath, "*.mp3");
            foreach (var file in files)
            {
                await TryProcessFileAsync(file);
            }
        });
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        // Simple delay to ensure file is completely written (primitive debounce)
        Task.Run(async () =>
        {
            await Task.Delay(1000);
            await TryProcessFileAsync(e.FullPath);
        });
    }

    private async Task TryProcessFileAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;

        try
        {
            // Try to open for read to ensure it's not locked by another process
            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Close();
            }

            var date = DateOnly.FromDateTime(DateTime.Now);
            var entry = await _processor.ProcessAudioAsync(filePath, date, null, CancellationToken.None);
            EntryProcessed?.Invoke(entry);
        }
        catch
        {
            // If file is locked or fails, maybe a retry logic could be added,
            // but for now we ignore and it stays in the queue or fails silently.
            // A more robust implementation might use a queue or Polly.
        }
    }

    public void Dispose()
    {
        if (_watcher != null)
        {
            _watcher.Created -= OnFileCreated;
            _watcher.Dispose();
            _watcher = null;
        }
    }
}