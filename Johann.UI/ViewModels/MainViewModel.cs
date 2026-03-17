using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Johann.Domain.Entities;
using Johann.UI.Views;
using Microsoft.Win32;

namespace Johann.UI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IEntryRepository _repository;
    private readonly IEnumerable<IEntryRenderer> _renderers;
    private readonly IEntryProcessor _processor;
    private readonly string _outputRoot;
    private readonly ISettingsRepository _settingsRepo;
    private readonly SettingsHolder _settingsHolder;

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

    // Filter & Sort
    [ObservableProperty] private bool _showOnlyPending = false;
    [ObservableProperty] private SortMode _currentSort = SortMode.ById;

    public bool IsSortById => CurrentSort == SortMode.ById;
    public bool IsSortByProject => CurrentSort == SortMode.ByProjectThenId;

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
        _detail = new EntryDetailViewModel(renderers, outputRoot, processor, repository);
        _detail.EntryStatusChanged += changedEntry => { _ = LoadEntriesAsync(SelectedDateItem?.Date); };
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
    }

    partial void OnShowOnlyPendingChanged(bool value)
    {
        _ = LoadEntriesAsync(SelectedDateItem?.Date);
    }

    partial void OnCurrentSortChanged(SortMode value)
    {
        OnPropertyChanged(nameof(IsSortById));
        OnPropertyChanged(nameof(IsSortByProject));
        _ = LoadEntriesAsync(SelectedDateItem?.Date);
    }

    private async Task LoadEntriesAsync(DateOnly? date)
    {
        Entries.Clear();
        SelectedEntry = null;

        if (date is null) return;

        IsLoading = true;
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
        }
        catch (Exception ex)
        {
            ErrorMessage = $"Fehler beim Laden: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    private IEnumerable<Entry> ApplySort(IEnumerable<Entry> entries) => CurrentSort switch
    {
        SortMode.ByProjectThenId => entries.OrderBy(e => e.ProjectName).ThenBy(e => e.SequenceNumber),
        _ => entries.OrderBy(e => e.SequenceNumber),
    };

    // ── Commands ──────────────────────────────────────────────────────────────

    [RelayCommand]
    private void SortById() => CurrentSort = SortMode.ById;

    [RelayCommand]
    private void SortByProject() => CurrentSort = SortMode.ByProjectThenId;

    [RelayCommand]
    private async Task AddEntry()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var todayEntries = await _repository.GetEntriesForDateAsync(today);
        var nextSeq = todayEntries.Count + 1;

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

        RefreshAfterEntry(entry);
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

            var progress = new Progress<ProcessingProgress>(p =>
                ErrorMessage = $"{prefix}{fileLabel}: {p.Stage} ({p.StepIndex}/{p.TotalSteps})");

            try
            {
                var entry = await _processor.ProcessAudioAsync(filePath, today, progress);
                RefreshAfterEntry(entry);
            }
            catch (Exception ex)
            {
                // Non-fatal: log per-file error, continue with remaining files
                ErrorMessage = $"{prefix}{fileLabel}: Fehler – {ex.Message}";
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
        var settingsVm = new SettingsViewModel(_settingsRepo, _settingsHolder);
        var window = new SettingsView(settingsVm)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        window.Show(); // non-modal — user can keep working
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    /// <summary>
    /// Called by the audio watcher (background thread) after it finishes processing a file.
    /// Must be called on the UI thread — caller is responsible for dispatching.
    /// </summary>
    public void NotifyEntryProcessed(Entry entry) => RefreshAfterEntry(entry);

    /// <summary>
    /// Inserts/selects the date and entry row in the UI after a new entry is created.
    /// </summary>
    private void RefreshAfterEntry(Entry entry)
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
            SelectedEntry = rowVm;
        }
        else
        {
            SelectedDateItem = existing;
        }
    }
}
