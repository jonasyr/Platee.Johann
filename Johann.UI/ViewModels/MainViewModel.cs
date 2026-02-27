using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace Johann.UI.ViewModels;

public sealed partial class MainViewModel : ObservableObject
{
    private readonly IEntryRepository _repository;
    private readonly IEnumerable<IEntryRenderer> _renderers;

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

    // Status
    [ObservableProperty] private bool _isLoading;
    [ObservableProperty] private string _errorMessage = string.Empty;

    public string SelectedDateDisplay =>
        SelectedDateItem?.DisplayText ?? "Kein Datum gewählt";

    public MainViewModel(IEntryRepository repository, IEnumerable<IEntryRenderer> renderers)
    {
        _repository = repository;
        _renderers  = renderers;
        _detail     = new EntryDetailViewModel(renderers);
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

    private async Task LoadEntriesAsync(DateOnly? date)
    {
        Entries.Clear();
        SelectedEntry = null;

        if (date is null) return;

        IsLoading = true;
        try
        {
            var entries = await _repository.GetEntriesForDateAsync(date.Value);
            foreach (var entry in entries)
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

    [RelayCommand]
    private void AddEntry()
    {
        // Phase 2: open new-entry dialog
        ErrorMessage = "Neue Einträge werden in Phase 2 implementiert.";
    }
}
