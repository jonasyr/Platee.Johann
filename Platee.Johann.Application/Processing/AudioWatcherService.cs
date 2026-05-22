namespace Platee.Johann.Application.Processing;

using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;

/// <summary>
/// Monitors a target directory for new MP3 files and automatically processes them.
/// </summary>
public sealed class AudioWatcherService : IDisposable
{
    private readonly IEntryProcessor processor;
    private readonly SettingsHolder settings;
    private FileSystemWatcher? watcher;
    private readonly SemaphoreSlim processLock = new(1, 1);

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
        this.processor = processor;
        this.settings = settings;
    }

    public void Start()
    {
        var inputPath = this.settings.Current.Quellverzeichnis;
        if (string.IsNullOrWhiteSpace(inputPath))
        {
            return;
        }

        try
        {
            Directory.CreateDirectory(inputPath);

            // Process existing files first
            this.ProcessExistingFiles(inputPath);

            this.watcher = new FileSystemWatcher(inputPath, "*.mp3")
            {
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime,
                EnableRaisingEvents = true,
            };

            this.watcher.Created += this.OnFileCreated;
        }
        catch (Exception ex)
        {
            this.watcher?.Dispose();
            this.watcher = null;
            this.EntryProcessingFailed?.Invoke(inputPath, ex);
        }
    }

    private void ProcessExistingFiles(string inputPath)
    {
        // Fire and forget so we don't block startup
        _ = Task.Run(async () =>
        {
            try
            {
                var files = Directory.GetFiles(inputPath, "*.mp3");
                foreach (var file in files)
                {
                    await this.TryProcessFileAsync(file);
                }
            }
            catch (Exception ex)
            {
                this.EntryProcessingFailed?.Invoke(inputPath, ex);
            }
        });
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        // Simple delay to ensure file is completely written (primitive debounce)
        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(1000);
                await this.TryProcessFileAsync(e.FullPath);
            }
            catch (Exception ex)
            {
                this.EntryProcessingFailed?.Invoke(e.FullPath, ex);
            }
        });
    }

    private async Task TryProcessFileAsync(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return;
        }

        await this.processLock.WaitAsync();
        try
        {
            if (!File.Exists(filePath))
            {
                return; // may have been processed while waiting
            }

            if (!this.processor.CanProcess)
            {
                throw new InvalidOperationException(
                    "Kein API-Schlüssel konfiguriert. .env-Datei in Dokumente\\Johann ablegen.");
            }

            using (var stream = File.Open(filePath, FileMode.Open, FileAccess.Read, FileShare.None))
            {
                stream.Close();
            }

            var date = DateOnly.FromDateTime(DateTime.Now);
            var progress = new Progress<ProcessingProgress>(p =>
                this.EntryProcessingProgress?.Invoke(filePath, p));
            var entry = await this.processor.ProcessAudioAsync(filePath, date, progress, CancellationToken.None);
            this.EntryProcessed?.Invoke(filePath, entry);
        }
        catch (Exception ex)
        {
            this.EntryProcessingFailed?.Invoke(filePath, ex);
        }
        finally
        {
            this.processLock.Release();
        }
    }

    public void Dispose()
    {
        if (this.watcher != null)
        {
            this.watcher.Created -= this.OnFileCreated;
            this.watcher.Dispose();
            this.watcher = null;
        }

        this.processLock.Dispose();
    }
}
