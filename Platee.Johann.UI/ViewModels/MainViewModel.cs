namespace Platee.Johann.UI.ViewModels;

using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Services;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.UI.Views;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IEntryRepository repository;
    private readonly IEnumerable<IEntryRenderer> renderers;
    private readonly IEntryProcessor processor;
    private readonly string outputRoot;
    private readonly ISettingsRepository settingsRepo;
    private readonly IPromptSettingsRepository localPromptRepo;
    private readonly SettingsHolder persistedSettingsHolder;
    private readonly SettingsHolder runtimeSettingsHolder;
    private readonly IReadOnlyList<StartupPathIssue> startupPathIssues;
    private readonly List<DateItemViewModel> allDates = [];
    private bool suppressDateSelectionChanged;
    private SettingsViewModel? settingsViewModel;
    private SettingsView? settingsWindow;

    // Left pane — DateItemViewModel wraps DateOnly and provides DisplayText
    public ObservableCollection<DateItemViewModel> AvailableDates { get; } = [];

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(SelectedDateDisplay))]
    private DateItemViewModel? selectedDateItem;

    // Center pane
    public ObservableCollection<EntryRowViewModel> Entries { get; } = [];

    [ObservableProperty]
    private EntryRowViewModel? selectedEntry;

    // Right pane
    [ObservableProperty]
    private EntryDetailViewModel detail;

    // Status — used for progress messages; IsLoading only for initial data loads
    [ObservableProperty]
    private bool isLoading;
    [ObservableProperty]
    private string errorMessage = string.Empty;

    // Processing state — drives top bar spinner + status text
    [ObservableProperty]
    private bool isProcessing;
    [ObservableProperty]
    private string statusText = "Bereit";
    [ObservableProperty]
    private bool isProcessLogOpen;

    public ToastsViewModel Toasts { get; } = new();
    private readonly Dictionary<string, ToastItem> runningToasts = [];

    public ObservableCollection<ProcessLogItem> ProcessLog { get; } = [];

    // Filter & Sort
    [ObservableProperty]
    private bool showOnlyPending = false;
    [ObservableProperty]
    private SortMode currentSort = SortMode.ById;
    [ObservableProperty]
    private bool isSortReversed;

    public SectionVisibilityViewModel Sections { get; } = new();

    public bool IsSortById => this.CurrentSort == SortMode.ById;

    public bool IsSortByProject => this.CurrentSort == SortMode.ByProjectThenId;

    public string SortBy => this.IsSortByProject ? "project" : "id";

    public string SortDir => this.IsSortReversed ? "desc" : "asc";

    public string SortDirectionArrow => this.IsSortReversed ? "↓" : "↑";

    public string SortByIdButtonLabel => this.IsSortById ? $"{this.SortDirectionArrow} Nr" : "Nr";

    public string SortByProjectButtonLabel => this.IsSortByProject ? $"{this.SortDirectionArrow} Projekt" : "Projekt";

    public string SortByIdLabel => this.IsSortById ? (this.IsSortReversed ? "ID ↑" : "ID ↓") : "ID";

    public string SortByProjectLabel => this.IsSortByProject ? (this.IsSortReversed ? "Projekt ↑" : "Projekt ↓") : "Projekt";

    public string SelectedDateDisplay =>
        this.SelectedDateItem?.Date.ToString("dd.MM.yy") ?? "Kein Datum gewählt";

    public bool CanAddAudio => this.processor.CanProcess;

    public bool IsApiKeyMissing => !this.processor.CanProcess;

    public bool HasInputPathIssue => Finding04State.FindMissingInputPathIssue(this.startupPathIssues) is not null;

    public string MissingInputPathDisplay =>
        Finding04State.FindMissingInputPathIssue(this.startupPathIssues)?.ConfiguredPath ?? string.Empty;

    public bool HasRunningJobs => this.ProcessLog.Any(x => x.IsRunning);

    public string OutputPathDisplay => this.outputRoot;

    public string WhisperVersion => "Whisper whisper-1";

    public MainViewModel(IEntryRepository repository, IEnumerable<IEntryRenderer> renderers,
                         string outputRoot, IEntryProcessor processor,
                         ISettingsRepository settingsRepo, IPromptSettingsRepository localPromptRepo,
                         SettingsHolder persistedSettingsHolder,
                         SettingsHolder runtimeSettingsHolder, IReadOnlyList<StartupPathIssue>? startupPathIssues = null)
    {
        this.repository = repository;
        this.renderers = renderers;
        this.outputRoot = outputRoot;
        this.processor = processor;
        this.settingsRepo = settingsRepo;
        this.localPromptRepo = localPromptRepo;
        this.persistedSettingsHolder = persistedSettingsHolder;
        this.runtimeSettingsHolder = runtimeSettingsHolder;
        this.startupPathIssues = startupPathIssues ?? [];
        this.detail = new EntryDetailViewModel(renderers, outputRoot, processor, repository, this.Sections,
            addLog: this.AddProcessLog,
            completeLog: this.CompleteProcessLog,
            updateStatus: s => System.Windows.Application.Current.Dispatcher.Invoke(() => this.StatusText = s));
        this.detail.EntryStatusChanged += entry =>
        {
            _ = this.LoadEntriesAsync(this.SelectedDateItem?.Date);
            _ = this.RecalculatePendingCountsAsync();
        };
    }

    public async Task InitializeAsync(CancellationToken ct = default)
    {
        this.IsLoading = true;
        this.ErrorMessage = string.Empty;
        try
        {
            var dates = await this.repository.GetAvailableDatesAsync(ct);
            this.allDates.Clear();
            foreach (var d in dates)
            {
                this.allDates.Add(new DateItemViewModel(d));
            }

            await this.RecalculatePendingCountsAsync(ct);
            this.RefreshAvailableDatesView();

            if (this.SelectedDateItem is null && this.AvailableDates.Count > 0)
            {
                this.SelectedDateItem = this.AvailableDates[0];
            }
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Fehler beim Laden der Daten: {ex.Message}";
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    partial void OnSelectedDateItemChanged(DateItemViewModel? value)
    {
        if (suppressDateSelectionChanged)
            return;

        _ = LoadEntriesAsync(value?.Date);
    }

    partial void OnSelectedEntryChanged(EntryRowViewModel? value)
    {
        Detail.Entry = value?.Entry;

        // Auto-select type-specific section; keep LongSummary + ProseSummary always true
        var type = value?.Entry.Type;
        Sections.ShowTaskList = type == EntryType.Aufgabe;
        Sections.ShowConversationNote = type == EntryType.Gesprächsnotiz;
        Sections.ShowEmailText = type == EntryType.EMail;
        Sections.ShowStundenzettelText = type == EntryType.Stundenzettel;
        Sections.ShowAnalogText = type == EntryType.Analog;
    }

    partial void OnShowOnlyPendingChanged(bool value)
    {
        RefreshAvailableDatesView();
        _ = LoadEntriesAsync(SelectedDateItem?.Date);
    }

    partial void OnCurrentSortChanged(SortMode value)
    {
        OnPropertyChanged(nameof(IsSortById));
        OnPropertyChanged(nameof(IsSortByProject));
        OnPropertyChanged(nameof(SortBy));
        OnPropertyChanged(nameof(SortDir));
        OnPropertyChanged(nameof(SortDirectionArrow));
        OnPropertyChanged(nameof(SortByIdButtonLabel));
        OnPropertyChanged(nameof(SortByProjectButtonLabel));
        OnPropertyChanged(nameof(SortByIdLabel));
        OnPropertyChanged(nameof(SortByProjectLabel));
    }

    partial void OnIsSortReversedChanged(bool value)
    {
        OnPropertyChanged(nameof(SortDir));
        OnPropertyChanged(nameof(SortDirectionArrow));
        OnPropertyChanged(nameof(SortByIdButtonLabel));
        OnPropertyChanged(nameof(SortByProjectButtonLabel));
        OnPropertyChanged(nameof(SortByIdLabel));
        OnPropertyChanged(nameof(SortByProjectLabel));
    }

    private async Task LoadEntriesAsync(DateOnly? date)
    {
        this.Entries.Clear();
        this.SelectedEntry = null;

        if (date is null)
        {
            return;
        }

        this.IsLoading = true;
        try
        {
            var entries = await this.repository.GetEntriesForDateAsync(date.Value);
            IEnumerable<Entry> filtered = this.ShowOnlyPending
                ? entries.Where(e => !e.IsDone)
                : entries;
            var sorted = this.ApplySort(filtered);
            foreach (var entry in sorted)
            {
                this.Entries.Add(new EntryRowViewModel(entry));
            }

            if (this.Entries.Count > 0)
            {
                this.SelectedEntry = this.Entries[0];
            }

            await this.RecalculatePendingCountsAsync();
        }
        catch (Exception ex)
        {
            this.ErrorMessage = $"Fehler beim Laden: {ex.Message}";
        }
        finally
        {
            this.IsLoading = false;
        }
    }

    private IEnumerable<Entry> ApplySort(IEnumerable<Entry> entries)
    {
        IEnumerable<Entry> sorted = this.CurrentSort switch
        {
            SortMode.ByProjectThenId => entries.OrderBy(e => e.ProjectName).ThenBy(e => e.SequenceNumber),
            _ => entries.OrderBy(e => e.SequenceNumber),
        };
        return this.IsSortReversed ? sorted.Reverse() : sorted;
    }

    // ── Process log helpers ────────────────────────────────────────────────────
    public ProcessLogItem AddProcessLog(string message, bool isRunning = false)
    {
        var item = new ProcessLogItem(message, DateTime.Now, isRunning);
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            this.ProcessLog.Insert(0, item);
            this.IsProcessing = this.ProcessLog.Any(x => x.IsRunning);
            this.OnPropertyChanged(nameof(this.HasRunningJobs));
            if (isRunning)
            {
                this.StatusText = message;
                var toast = this.Toasts.ShowRunning(message);
                this.runningToasts[item.Key] = toast;
            }
            else
            {
                var tone = ToastToneHelper.DeriveFromAdd(message);
                this.Toasts.Show(message, tone);
            }
        });
        return item;
    }

    public void UpdateToastProgress(string message)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            this.StatusText = message;
        });
    }

    public void CompleteProcessLog(ProcessLogItem item, string resultMessage)
    {
        System.Windows.Application.Current.Dispatcher.Invoke(() =>
        {
            item.Complete(resultMessage);
            this.IsProcessing = this.ProcessLog.Any(x => x.IsRunning);
            this.OnPropertyChanged(nameof(this.HasRunningJobs));
            this.StatusText = this.IsProcessing ? this.StatusText : "Bereit";

            var tone = ToastToneHelper.DeriveFromCompletion(resultMessage);
            if (this.runningToasts.TryGetValue(item.Key, out var toast))
            {
                this.Toasts.Complete(toast, resultMessage, tone);
                this.runningToasts.Remove(item.Key);
            }
            else
            {
                this.Toasts.Show(resultMessage, tone);
            }
        });
    }

    [RelayCommand]
    private void OpenProcessDetail() => this.IsProcessLogOpen = !this.IsProcessLogOpen;

    [RelayCommand]
    private void ClearProcessLog()
    {
        this.ProcessLog.Clear();
        this.IsProcessing = false;
        this.OnPropertyChanged(nameof(this.HasRunningJobs));
    }

    [RelayCommand]
    private void RemoveCompletedLogs()
    {
        var completed = this.ProcessLog.Where(x => !x.IsRunning).ToList();
        foreach (var item in completed)
        {
            this.ProcessLog.Remove(item);
        }

        this.OnPropertyChanged(nameof(this.HasRunningJobs));
    }

    // ── Commands ──────────────────────────────────────────────────────────────
    [RelayCommand]
    private void SortById()
    {
        if (this.CurrentSort == SortMode.ById)
        {
            this.IsSortReversed = !this.IsSortReversed;
        }
        else
        {
            this.IsSortReversed = false;
            this.CurrentSort = SortMode.ById;
        }

        this.OnPropertyChanged(nameof(this.SortByIdLabel));
        this.OnPropertyChanged(nameof(this.SortByProjectLabel));
        _ = this.LoadEntriesAsync(this.SelectedDateItem?.Date);
    }

    [RelayCommand]
    private void SortByProject()
    {
        if (this.CurrentSort == SortMode.ByProjectThenId)
        {
            this.IsSortReversed = !this.IsSortReversed;
        }
        else
        {
            this.IsSortReversed = false;
            this.CurrentSort = SortMode.ByProjectThenId;
        }

        this.OnPropertyChanged(nameof(this.SortByIdLabel));
        this.OnPropertyChanged(nameof(this.SortByProjectLabel));
        _ = this.LoadEntriesAsync(this.SelectedDateItem?.Date);
    }

    [RelayCommand]
    private void ResetSections()
    {
        this.ResetSectionsToDefaults(this.SelectedEntry?.Entry.Type);
    }

    [RelayCommand]
    private async Task AddEntry()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var nextSeq = await this.repository.GetNextSequenceNumberAsync(today);

        var dialogVm = new NewEntryViewModel(nextSeq);
        var dialog = new NewEntryView(dialogVm)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };

        if (dialog.ShowDialog() != true || dialogVm.CreatedEntry is null)
        {
            return;
        }

        var entry = dialogVm.CreatedEntry;

        // Persist first so the entry is visible even if AI fails
        await this.repository.SaveAsync(entry);

        // If AI is available and the user entered content, auto-generate summaries
        if (this.processor.CanProcess && !string.IsNullOrWhiteSpace(entry.Transcript))
        {
            try
            {
                this.ErrorMessage = "Generiere KI-Zusammenfassungen…";
                entry = await this.processor.ReprocessAsync(entry);
                this.ErrorMessage = string.Empty;
            }
            catch (Exception ex)
            {
                this.ErrorMessage = $"KI-Fehler: {ex.Message}";
            }
        }

        await this.RefreshAfterEntryAsync(entry);
    }

    [RelayCommand]
    private async Task AddAudio()
    {
        if (!this.processor.CanProcess)
        {
            this.ErrorMessage = "Kein OpenAI API-Key konfiguriert. OPENAI_API_KEY setzen oder .env Datei erstellen.";
            return;
        }

        var dialog = new OpenFileDialog
        {
            Filter = "MP3-Dateien|*.mp3|Alle Audiodateien|*.mp3;*.m4a;*.wav|Alle Dateien|*.*",
            Title = "MP3-Dateien für Transkription auswählen",
            Multiselect = true,
        };

        if (dialog.ShowDialog() != true)
        {
            return;
        }

        var files = dialog.FileNames;
        var today = DateOnly.FromDateTime(DateTime.Today);

        // No IsLoading here — keeps UI accessible; progress shown in status bar
        this.ErrorMessage = string.Empty;

        for (int i = 0; i < files.Length; i++)
        {
            var filePath = files[i];
            var fileLabel = Path.GetFileName(filePath);
            var prefix = files.Length > 1 ? $"[{i + 1}/{files.Length}] " : string.Empty;

            var logItem = this.AddProcessLog($"{prefix}{fileLabel}: Audiodatei wird verarbeitet…", isRunning: true);

            var progress = new Progress<ProcessingProgress>(p =>
            {
                this.ErrorMessage = $"{prefix}{fileLabel}: {p.Stage}";
                System.Windows.Application.Current.Dispatcher.Invoke(() => this.StatusText = $"{prefix}{fileLabel}: {p.Stage}");
            });

            try
            {
                var entry = await this.processor.ProcessAudioAsync(filePath, today, progress);
                await this.RefreshAfterEntryAsync(entry);
                this.CompleteProcessLog(logItem, "Fertig");
            }
            catch (Exception ex)
            {
                // Non-fatal: log per-file error, continue with remaining files
                this.ErrorMessage = $"{prefix}{fileLabel}: Fehler – {ex.Message}";
                this.CompleteProcessLog(logItem, $"Fehler: {ex.Message}");
            }
        }

        if (files.Length > 1 && string.IsNullOrEmpty(this.ErrorMessage))
        {
            this.ErrorMessage = $"{files.Length} Dateien verarbeitet.";
        }
        else if (files.Length == 1 && string.IsNullOrEmpty(this.ErrorMessage))
        {
            this.ErrorMessage = string.Empty;
        }
    }

    [RelayCommand]
    private void OpenSettings()
    {
        if (this.settingsWindow is { IsLoaded: true })
        {
            if (this.settingsWindow.WindowState == System.Windows.WindowState.Minimized)
            {
                this.settingsWindow.WindowState = System.Windows.WindowState.Normal;
            }

            this.settingsWindow.Activate();
            return;
        }

        this.settingsViewModel ??= new SettingsViewModel(
            this.settingsRepo,
            this.localPromptRepo,
            this.persistedSettingsHolder,
            this.runtimeSettingsHolder,
            this.startupPathIssues);
        this.settingsWindow = new SettingsView(this.settingsViewModel)
        {
            Owner = System.Windows.Application.Current.MainWindow,
        };
        this.settingsWindow.Closed += (_, _) => this.settingsWindow = null;
        this.settingsWindow.Show(); // non-modal with single-instance behavior
    }

    [RelayCommand]
    private void OpenHandbook()
    {
        var handbuchPath = FindHandbookPath();
        if (handbuchPath is not null)
        {
            System.Diagnostics.Process.Start(
                new System.Diagnostics.ProcessStartInfo(handbuchPath) { UseShellExecute = true });
            return;
        }

        System.Windows.MessageBox.Show(
            "Dokumentation nicht gefunden:\nHANDBUCH.html",
            "Hilfe",
            System.Windows.MessageBoxButton.OK,
            System.Windows.MessageBoxImage.Information);
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the audio watcher (background thread) after it finishes processing a file.
    /// Must be called on the UI thread — caller is responsible for dispatching.
    /// </summary>
    public void NotifyEntryProcessed(Entry entry) => _ = this.RefreshAfterEntryAsync(entry);

    /// <summary>
    /// Inserts/selects the date and entry row in the UI after a new entry is created.
    /// </summary>
    private async Task RefreshAfterEntryAsync(Entry entry)
    {
        var entryDate = DateOnly.FromDateTime(entry.CreatedAt.DateTime);
        var existing = this.AvailableDates.FirstOrDefault(d => d.Date == entryDate);

        if (existing is null)
        {
            var newDateItem = new DateItemViewModel(entryDate);
            this.allDates.Add(newDateItem);
            this.RefreshAvailableDatesView();
            this.SelectedDateItem = this.AvailableDates.FirstOrDefault(d => d.Date == entryDate) ?? newDateItem;
        }
        else if (this.SelectedDateItem?.Date == entryDate)
        {
            var rowVm = new EntryRowViewModel(entry);
            this.Entries.Add(rowVm);
        }
        else
        {
            this.SelectedDateItem = existing;
        }

        await this.RecalculatePendingCountsAsync();
    }

    private async Task RecalculatePendingCountsAsync(CancellationToken ct = default)
    {
        foreach (var dateItem in this.allDates)
        {
            ct.ThrowIfCancellationRequested();
            var entries = await this.repository.GetEntriesForDateAsync(dateItem.Date, ct);
            var pending = entries.Count(e => !e.IsDone);
            dateItem.UpdateCounts(entries.Count, pending);
        }

        this.RefreshAvailableDatesView();
    }

    private void RefreshAvailableDatesView()
    {
        this.suppressDateSelectionChanged = true;
        try
        {
            var selectedDate = this.SelectedDateItem?.Date;
            var visibleDates = this.allDates
                .Where(d => !this.ShowOnlyPending || d.PendingCount > 0)
                .OrderByDescending(d => d.Date)
                .ToList();

            this.AvailableDates.Clear();
            foreach (var item in visibleDates)
            {
                this.AvailableDates.Add(item);
            }

            if (selectedDate is not null)
            {
                var selectedVisible = this.AvailableDates.FirstOrDefault(d => d.Date == selectedDate.Value);
                if (!ReferenceEquals(selectedVisible, this.SelectedDateItem))
                {
                    this.SelectedDateItem = selectedVisible;
                }
            }

            if (this.SelectedDateItem is null && this.AvailableDates.Count > 0)
            {
                this.SelectedDateItem = this.AvailableDates[0];
            }
        }
        finally
        {
            this.suppressDateSelectionChanged = false;
        }
    }

    private static string? FindHandbookPath()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "HANDBUCH.html"),
            Path.Combine(Environment.CurrentDirectory, "HANDBUCH.html"),
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "HANDBUCH.html")),
        };

        return candidates.FirstOrDefault(File.Exists);
    }

    private void ResetSectionsToDefaults(EntryType? type)
    {
        this.Sections.ShowLongSummary = true;
        this.Sections.ShowProseSummary = true;
        this.Sections.ShowTaskList = true;
        this.Sections.ShowConversationNote = true;
        this.Sections.ShowEmailText = false;
        this.Sections.ShowStundenzettelText = false;
        this.Sections.ShowAnalogText = false;
        this.Sections.ShowTranscript = true;

        this.Sections.ShowTaskList = type == EntryType.Aufgabe;
        this.Sections.ShowConversationNote = type == EntryType.Gesprächsnotiz;
        this.Sections.ShowEmailText = type == EntryType.EMail;
        this.Sections.ShowStundenzettelText = type == EntryType.Stundenzettel;
        this.Sections.ShowAnalogText = type == EntryType.Analog;
    }
}
