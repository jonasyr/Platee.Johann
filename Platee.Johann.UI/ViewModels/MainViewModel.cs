using System.Collections.ObjectModel;
using System.IO;
using System.Windows.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Platee.Johann.Application.Services;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Microsoft.Win32;
using Platee.Johann.UI.Views;

namespace Platee.Johann.UI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IEntryRepository _repository;
    private readonly IEnumerable<IEntryRenderer> _renderers;
    private readonly IEntryProcessor _processor;
    private readonly string _outputRoot;
    private readonly ISettingsRepository _settingsRepo;
    private readonly SettingsHolder _settingsHolder;
    private SettingsViewModel? _settingsViewModel;
    private SettingsView? _settingsWindow;

    // Left pane — DateItemViewModel wraps DateOnly and provides DisplayText
    public ObservableCollection<DateItemViewModel> AvailableDates { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDateDisplay))]
    private DateItemViewModel? _selectedDateItem;

    // Center pane
    public ObservableCollection<EntryRowViewModel> Entries { get; } = [];

    [ObservableProperty] private EntryRowViewModel? _selectedEntry;

    // Right pane
    [ObservableProperty] private EntryDetailViewModel _detail;

    // Status — used for progress messages; IsLoading only for initial data loads
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;

    // Processing state — drives top bar spinner + status text
    [ObservableProperty] private bool _isProcessing;
    [ObservableProperty] private string _statusText = "Bereit";
    [ObservableProperty] private bool _isProcessLogOpen;

    // Toast notification
    [ObservableProperty] private string _toastMessage = string.Empty;
    [ObservableProperty] private bool _isToastRunning;
    [ObservableProperty] private bool _isToastVisible;
    private DispatcherTimer? _toastTimer;

    public ObservableCollection<ProcessLogItem> ProcessLog { get; } = [];

    // Filter & Sort
    [ObservableProperty] private bool _showOnlyPending = false;
    [ObservableProperty] private SortMode _currentSort = SortMode.ById;
    [ObservableProperty] private bool _isSortReversed;

    public SectionVisibilityViewModel Sections { get; } = new();

    public bool IsSortById => CurrentSort == SortMode.ById;
    public bool IsSortByProject => CurrentSort == SortMode.ByProjectThenId;

    public string SortByIdLabel => IsSortById ? (IsSortReversed ? "ID ↑" : "ID ↓") : "ID";
    public string SortByProjectLabel => IsSortByProject ? (IsSortReversed ? "Projekt ↑" : "Projekt ↓") : "Projekt";

    public string SelectedDateDisplay =>
        SelectedDateItem?.DisplayText ?? "Kein Datum gewählt";

    public bool CanAddAudio => _processor.CanProcess;

    public MainViewModel(IEntryRepository repository, IEnumerable<IEntryRenderer> renderers,
                         string outputRoot, IEntryProcessor processor,
                         ISettingsRepository settingsRepo, SettingsHolder settingsHolder)
    {
        _repository = repository;
        _renderers = renderers;
        _outputRoot = outputRoot;
        _processor = processor;
        _settingsRepo = settingsRepo;
        _settingsHolder = settingsHolder;
        _detail = new EntryDetailViewModel(renderers, outputRoot, processor, repository, Sections,
            addLog: AddProcessLog,
            completeLog: CompleteProcessLog,
            updateStatus: s => System.Windows.Application.Current.Dispatcher.Invoke(() => StatusText = s));
        _detail.EntryStatusChanged += entry =>
        {
            _ = LoadEntriesAsync(SelectedDateItem?.Date);
            _ = RecalculatePendingCountsAsync();
        };
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        IsLoading = true;
        ErrorMessage = string.Empty;
        try
        {
            var dates = await _repository.GetAvailableDatesAsync(ct);
            AvailableDates.Clear();
            foreach (var d in dates)
                AvailableDates.Add(new DateItemViewModel(d));

            if (AvailableDates.Count > 0)
                SelectedDateItem = AvailableDates[0];

            await RecalculatePendingCountsAsync(ct);
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Fehler beim Laden der Daten: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    partial void OnSelectedDateItemChanged(DateItemViewModel? value)
    {
        _ = LoadEntriesAsync(value?.Date);
    }

    partial void OnSelectedEntryChanged(EntryRowViewModel? value)
    {
        Detail.Entry = value?.Entry;

        // Auto-select type-specific section; keep LongSummary + ProseSummary always true
        var type = value?.Entry.Type;
        Sections.ShowTaskList          = type == EntryType.Aufgabe;
        Sections.ShowConversationNote  = type == EntryType.Gesprächsnotiz;
        Sections.ShowEmailText         = type == EntryType.EMail;
        Sections.ShowStundenzettelText = type == EntryType.Stundenzettel;
        Sections.ShowAnalogText        = type == EntryType.Analog;
    }

    partial void OnShowOnlyPendingChanged(bool value)
    {
        _ = LoadEntriesAsync(SelectedDateItem?.Date);
    }

    partial void OnCurrentSortChanged(SortMode value)
    {
        OnPropertyChanged(nameof(IsSortById));
        OnPropertyChanged(nameof(IsSortByProject));
        OnPropertyChanged(nameof(SortByIdLabel));
        OnPropertyChanged(nameof(SortByProjectLabel));
    }

    partial void OnIsSortReversedChanged(bool value)
    {
        OnPropertyChanged(nameof(SortByIdLabel));
        OnPropertyChanged(nameof(SortByProjectLabel));
    }

    private async Task LoadEntriesAsync(DateOnly? date)
    {
        Entries.Clear();
        SelectedEntry = null;

        if (date is null) return;

        IsLoading = true;
        var logItem = AddProcessLog("Einträge laden…", isRunning: true);
        try
        {
            var entries = await _repository.GetEntriesForDateAsync(date.Value);
            IEnumerable<Entry> filtered = ShowOnlyPending
                ? entries.Where(e => !e.IsDone)
                : entries;
            var sorted = ApplySort(filtered);
            foreach (var entry in sorted)
                Entries.Add(new EntryRowViewModel(entry));

            if (Entries.Count > 0)
                SelectedEntry = Entries[0];

            await RecalculatePendingCountsAsync();
            CompleteProcessLog(logItem, $"{Entries.Count} Einträge geladen");
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Fehler beim Laden: {ex.Message}";
            CompleteProcessLog(logItem, $"Fehler: {ex.Message}");
        }
        finally
        {
            IsLoading = false;
        }
    }

    private IEnumerable<Entry> ApplySort(IEnumerable<Entry> entries)
    {
        IEnumerable<Entry> sorted = CurrentSort switch
        {
            SortMode.ByProjectThenId => entries.OrderBy(e => e.ProjectName).ThenBy(e => e.SequenceNumber),
            _ => entries.OrderBy(e => e.SequenceNumber),
        };
        return IsSortReversed ? sorted.Reverse() : sorted;
    }

    // ── Process log helpers ────────────────────────────────────────────────────

    public ProcessLogItem AddProcessLog(string message, bool isRunning = false)
    {
        var item = new ProcessLogItem(message, DateTime.Now, isRunning);
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            ProcessLog.Insert(0, item);
            IsProcessing = ProcessLog.Any(x => x.IsRunning);
            if (isRunning) StatusText = message;
            ToastMessage = message;
            IsToastRunning = isRunning;
            IsToastVisible = true;
            StartToastDismissTimer();
        });
        return item;
    }

    /// <summary>Updates the toast text in-place (e.g. for progress stage changes) without adding a new log entry.</summary>
    public void UpdateToastProgress(string message)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            StatusText = message;
            ToastMessage = message;
            IsToastVisible = true;
            StartToastDismissTimer();
        });
    }

    public void CompleteProcessLog(ProcessLogItem item, string resultMessage)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            item.Complete(resultMessage);
            IsProcessing = ProcessLog.Any(x => x.IsRunning);
            StatusText = IsProcessing ? StatusText : "Bereit";
            ToastMessage = resultMessage;
            IsToastRunning = false;
            StartToastDismissTimer();
        });
    }

    [RelayCommand]
    private void OpenProcessDetail() => IsProcessLogOpen = !IsProcessLogOpen;

    [RelayCommand]
    private void ClearProcessLog() => ProcessLog.Clear();

    [RelayCommand]
    private void RemoveCompletedLogs()
    {
        var completed = ProcessLog.Where(x => !x.IsRunning).ToList();
        foreach (var item in completed)
            ProcessLog.Remove(item);
    }

    [RelayCommand]
    private void DismissToast()
    {
        _toastTimer?.Stop();
        IsToastVisible = false;
    }

    private void StartToastDismissTimer()
    {
        _toastTimer?.Stop();
        _toastTimer = new DispatcherTimer(DispatcherPriority.Normal, Dispatcher.CurrentDispatcher)
            { Interval = TimeSpan.FromSeconds(3) };
        _toastTimer.Tick += (_, _) => { IsToastVisible = false; _toastTimer.Stop(); };
        _toastTimer.Start();
    }

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void SortById()
    {
        if (CurrentSort == SortMode.ById)
            IsSortReversed = !IsSortReversed;
        else
        {
            IsSortReversed = false;
            CurrentSort = SortMode.ById;
        }
        OnPropertyChanged(nameof(SortByIdLabel));
        OnPropertyChanged(nameof(SortByProjectLabel));
        _ = LoadEntriesAsync(SelectedDateItem?.Date);
    }

    [RelayCommand]
    private void SortByProject()
    {
        if (CurrentSort == SortMode.ByProjectThenId)
            IsSortReversed = !IsSortReversed;
        else
        {
            IsSortReversed = false;
            CurrentSort = SortMode.ByProjectThenId;
        }
        OnPropertyChanged(nameof(SortByIdLabel));
        OnPropertyChanged(nameof(SortByProjectLabel));
        _ = LoadEntriesAsync(SelectedDateItem?.Date);
    }

    [RelayCommand]
    private async Task AddEntry()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var nextSeq = await _repository.GetNextSequenceNumberAsync(today);

        var dialogVm = new NewEntryViewModel(nextSeq);
        var dialog = new NewEntryView(dialogVm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };

        if (dialog.ShowDialog() != true || dialogVm.CreatedEntry is null)
            return;

        var entry = dialogVm.CreatedEntry;

        // Persist first so the entry is visible even if AI fails
        await _repository.SaveAsync(entry);

        // If AI is available and the user entered content, auto-generate summaries
        if (_processor.CanProcess && !string.IsNullOrWhiteSpace(entry.Transcript))
        {
            try
            {
                ErrorMessage = "Generiere KI-Zusammenfassungen…";
                entry = await _processor.ReprocessAsync(entry);
                ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                ErrorMessage = $"KI-Fehler: {ex.Message}";
            }
        }

        await RefreshAfterEntryAsync(entry);
    }

    [RelayCommand]
    private async Task AddAudio()
    {
        if (!_processor.CanProcess)
        {
            ErrorMessage = "Kein OpenAI API-Key konfiguriert. OPENAI_API_KEY setzen oder .env Datei erstellen.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "MP3-Dateien|*.mp3|Alle Audiodateien|*.mp3;*.m4a;*.wav|Alle Dateien|*.*",
            Title = "MP3-Dateien für Transkription auswählen",
            Multiselect = true,
        };

        if (dialog.ShowDialog() != true) return;

        var files = dialog.FileNames;
        var today = DateOnly.FromDateTime(DateTime.Today);

        // No IsLoading here — keeps UI accessible; progress shown in status bar
        ErrorMessage = string.Empty;

        for (int i = 0; i < files.Length; i++)
        {
            var filePath = files[i];
            var fileLabel = Path.GetFileName(filePath);
            var prefix = files.Length > 1 ? $"[{i + 1}/{files.Length}] " : string.Empty;

            var logItem = AddProcessLog($"{prefix}{fileLabel}: Audiodatei wird verarbeitet…", isRunning: true);

            var progress = new Progress<ProcessingProgress>(p =>
            {
                ErrorMessage = $"{prefix}{fileLabel}: {p.Stage}";
                System.Windows.Application.Current.Dispatcher.Invoke(() => StatusText = $"{prefix}{fileLabel}: {p.Stage}");
            });

            try
            {
                var entry = await _processor.ProcessAudioAsync(filePath, today, progress);
                await RefreshAfterEntryAsync(entry);
                CompleteProcessLog(logItem, "Fertig");
            }
            catch (Exception ex)
            {
                // Non-fatal: log per-file error, continue with remaining files
                ErrorMessage = $"{prefix}{fileLabel}: Fehler – {ex.Message}";
                CompleteProcessLog(logItem, $"Fehler: {ex.Message}");
            }
        }

        if (files.Length > 1 && string.IsNullOrEmpty(ErrorMessage))
            ErrorMessage = $"{files.Length} Dateien verarbeitet.";
        else if (files.Length == 1 && string.IsNullOrEmpty(ErrorMessage))
            ErrorMessage = string.Empty;
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (_settingsWindow is { IsLoaded: true })
        {
            if (_settingsWindow.WindowState == System.Windows.WindowState.Minimized)
                _settingsWindow.WindowState = System.Windows.WindowState.Normal;

            _settingsWindow.Activate();
            return;
        }

        _settingsViewModel ??= new SettingsViewModel(_settingsRepo, _settingsHolder);
        _settingsWindow = new SettingsView(_settingsViewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show(); // non-modal with single-instance behavior
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the audio watcher (background thread) after it finishes processing a file.
    /// Must be called on the UI thread — caller is responsible for dispatching.
    /// </summary>
    public void NotifyEntryProcessed(Entry entry) => _ = RefreshAfterEntryAsync(entry);

    /// <summary>
    /// Inserts/selects the date and entry row in the UI after a new entry is created.
    /// </summary>
    private async Task RefreshAfterEntryAsync(Entry entry)
    {
        var entryDate = DateOnly.FromDateTime(entry.CreatedAt.DateTime);
        var existing = AvailableDates.FirstOrDefault(d => d.Date == entryDate);

        if (existing is null)
        {
            var newDateItem = new DateItemViewModel(entryDate);
            var insertAt = AvailableDates.TakeWhile(d => d.Date > entryDate).Count();
            AvailableDates.Insert(insertAt, newDateItem);
            SelectedDateItem = newDateItem;
        }
        else if (SelectedDateItem?.Date == entryDate)
        {
            var rowVm = new EntryRowViewModel(entry);
            Entries.Add(rowVm);
        }
        else
        {
            SelectedDateItem = existing;
        }

        await RecalculatePendingCountsAsync();
    }


    private async Task RecalculatePendingCountsAsync(CancellationToken ct = default)
    {
        foreach (var dateItem in AvailableDates.ToList())
        {
            ct.ThrowIfCancellationRequested();
            dateItem.PendingCount = await PendingCountCalculator.GetPendingCountForDateAsync(_repository, dateItem.Date, ct);
        }
    }
}
