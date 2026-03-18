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
    private readonly SemaphoreSlim _processLock = new(1, 1);

    /// <summary>
    /// Raised on a background thread after an audio file is successfully processed.
    /// Carries the source file path so subscribers can correlate with progress events.
    /// Subscribers must marshal to the UI thread themselves.
    /// </summary>
    public event Action<string, Platee.Johann.Domain.Entities.Entry>? EntryProcessed;
    public event Action<string, ProcessingProgress>? EntryProcessingProgress;
    public event Action<string, Exception>? EntryProcessingFailed;

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

        await _processLock.WaitAsync();
        try
        {
            if (!File.Exists(filePath)) return; // may have been processed while waiting

            if (!_processor.CanProcess)
                throw new InvalidOperationException(
                    "Kein API-Schlüssel konfiguriert. .env-Datei in Dokumente\\Johann ablegen.");

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
                stream.Close();

            var date = DateOnly.FromDateTime(DateTime.Now);
            var progress = new Progress<ProcessingProgress>(p =>
                EntryProcessingProgress?.Invoke(filePath, p));
            var entry = await _processor.ProcessAudioAsync(filePath, date, progress, CancellationToken.None);
            EntryProcessed?.Invoke(filePath, entry);
        }
        catch (Exception ex)
        {
            EntryProcessingFailed?.Invoke(filePath, ex);
        }
        finally
        {
            _processLock.Release();
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
        _processLock.Dispose();
    }
}