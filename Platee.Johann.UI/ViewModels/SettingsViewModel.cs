using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Microsoft.Win32;

namespace Platee.Johann.UI.ViewModels;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository _repository;
    private readonly SettingsHolder _holder;

    // ── User info ─────────────────────────────────────────────────────────────
    [ObservableProperty] private string _name = string.Empty;
    [ObservableProperty] private string _firma = string.Empty;

    // ── Directories ───────────────────────────────────────────────────────────
    [ObservableProperty] private string _quellverzeichnis = string.Empty;
    [ObservableProperty] private string _archivverzeichnis = string.Empty;
    [ObservableProperty] private string _ausgabeverzeichnis = string.Empty;

    // ── General prompts ───────────────────────────────────────────────────────
    [ObservableProperty] private string _systemMessage = string.Empty;
    [ObservableProperty] private string _abstractPrompt = string.Empty;
    [ObservableProperty] private string _structuredPrompt = string.Empty;
    [ObservableProperty] private string _prosePrompt = string.Empty;

    // ── Type-specific prompts ─────────────────────────────────────────────────
    [ObservableProperty] private string _emailPrompt = string.Empty;
    [ObservableProperty] private string _aufgabePrompt = string.Empty;
    [ObservableProperty] private string _gespraechsnotizPrompt = string.Empty;
    [ObservableProperty] private string _stundenzettelPrompt = string.Empty;
    [ObservableProperty] private string _analogPrompt = string.Empty;

    [ObservableProperty] private string _statusMessage = string.Empty;

    public SettingsViewModel(ISettingsRepository repository, SettingsHolder holder)
    {
        _repository = repository;
        _holder = holder;
        LoadFromHolder();
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        // Preserve all fields — only override what this UI actually exposes.
        var updated = _holder.Current with
        {
            Name = Name.Trim(),
            Firma = Firma.Trim(),
            Quellverzeichnis = Quellverzeichnis.Trim(),
            Archivverzeichnis = Archivverzeichnis.Trim(),
            Ausgabeverzeichnis = Ausgabeverzeichnis.Trim(),
            SystemMessage = SystemMessage.Trim(),
            AbstractPrompt = AbstractPrompt.Trim(),
            StructuredPrompt = StructuredPrompt.Trim(),
            ProsePrompt = ProsePrompt.Trim(),
            EmailPrompt = EmailPrompt.Trim(),
            AufgabePrompt = AufgabePrompt.Trim(),
            GespraechsnotizPrompt = GespraechsnotizPrompt.Trim(),
            StundenzettelPrompt = StundenzettelPrompt.Trim(),
            AnalogPrompt = AnalogPrompt.Trim(),
        };

        await _repository.SaveAsync(updated);
        _holder.Current = updated;
        StatusMessage = "✓ Einstellungen gespeichert.";
    }

    [RelayCommand]
    private void Reset()
    {
        var d = AppSettings.Default;
        Name = d.Name;
        Firma = d.Firma;
        Quellverzeichnis = d.Quellverzeichnis;
        Archivverzeichnis = d.Archivverzeichnis;
        Ausgabeverzeichnis = d.Ausgabeverzeichnis;
        SystemMessage = SummaryPrompts.SystemMessage;
        AbstractPrompt = SummaryPrompts.Abstract;
        StructuredPrompt = SummaryPrompts.Structured;
        ProsePrompt = SummaryPrompts.Prose;
        EmailPrompt = SummaryPrompts.Email;
        AufgabePrompt = SummaryPrompts.Aufgabe;
        GespraechsnotizPrompt = SummaryPrompts.Gespraechsnotiz;
        StundenzettelPrompt = SummaryPrompts.Stundenzettel;
        AnalogPrompt = SummaryPrompts.Analog;
        StatusMessage = "Standard-Werte wiederhergestellt – noch nicht gespeichert.";
    }

    [RelayCommand]
    private void BrowseQuell()
    {
        var path = PickFolder(Quellverzeichnis);
        if (path is not null) Quellverzeichnis = path;
    }

    [RelayCommand]
    private void BrowseArchiv()
    {
        var path = PickFolder(Archivverzeichnis);
        if (path is not null) Archivverzeichnis = path;
    }

    [RelayCommand]
    private void BrowseAusgabe()
    {
        var path = PickFolder(Ausgabeverzeichnis);
        if (path is not null) Ausgabeverzeichnis = path;
    }

    // ── Private ───────────────────────────────────────────────────────────────

    private void LoadFromHolder()
    {
        var s = _holder.Current;
        Name = s.Name;
        Firma = s.Firma;
        Quellverzeichnis = s.Quellverzeichnis;
        Archivverzeichnis = s.Archivverzeichnis;
        Ausgabeverzeichnis = s.Ausgabeverzeichnis;
        SystemMessage = s.SystemMessage;
        AbstractPrompt = s.AbstractPrompt;
        StructuredPrompt = s.StructuredPrompt;
        ProsePrompt = s.ProsePrompt;
        EmailPrompt = s.EmailPrompt;
        AufgabePrompt = s.AufgabePrompt;
        GespraechsnotizPrompt = s.GespraechsnotizPrompt;
        StundenzettelPrompt = s.StundenzettelPrompt;
        AnalogPrompt = s.AnalogPrompt;
    }

    private static string? PickFolder(string initialDir)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "Verzeichnis auswählen",
            InitialDirectory = Directory.Exists(initialDir) ? initialDir : string.Empty,
        };
        return dialog.ShowDialog() == true ? dialog.FolderName : null;
    }
}
