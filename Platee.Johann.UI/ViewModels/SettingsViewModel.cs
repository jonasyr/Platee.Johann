using System;
using System.Collections.Generic;
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
    [ObservableProperty] private SettingsSectionItem? _selectedSection;

    public IReadOnlyList<SettingsSectionItem> Sections { get; }

    public bool IsGeneralSelected => IsSelected(SectionGeneral);
    public bool IsPathsSelected => IsSelected(SectionPaths);
    public bool IsSystemMessageSelected => IsSelected(SectionSystemMessage);
    public bool IsAbstractSelected => IsSelected(SectionAbstract);
    public bool IsStructuredSelected => IsSelected(SectionStructured);
    public bool IsProseSelected => IsSelected(SectionProse);
    public bool IsEmailSelected => IsSelected(SectionEmail);
    public bool IsAufgabeSelected => IsSelected(SectionAufgabe);
    public bool IsGespraechsnotizSelected => IsSelected(SectionGespraechsnotiz);
    public bool IsStundenzettelSelected => IsSelected(SectionStundenzettel);
    public bool IsAnalogSelected => IsSelected(SectionAnalog);

    public SettingsViewModel(ISettingsRepository repository, SettingsHolder holder)
    {
        _repository = repository;
        _holder = holder;
        Sections = BuildSections();
        LoadFromHolder();
        SelectedSection = Sections[0];
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

    partial void OnSelectedSectionChanged(SettingsSectionItem? value)
    {
        OnPropertyChanged(nameof(IsGeneralSelected));
        OnPropertyChanged(nameof(IsPathsSelected));
        OnPropertyChanged(nameof(IsSystemMessageSelected));
        OnPropertyChanged(nameof(IsAbstractSelected));
        OnPropertyChanged(nameof(IsStructuredSelected));
        OnPropertyChanged(nameof(IsProseSelected));
        OnPropertyChanged(nameof(IsEmailSelected));
        OnPropertyChanged(nameof(IsAufgabeSelected));
        OnPropertyChanged(nameof(IsGespraechsnotizSelected));
        OnPropertyChanged(nameof(IsStundenzettelSelected));
        OnPropertyChanged(nameof(IsAnalogSelected));
    }

    private bool IsSelected(string sectionKey) =>
        string.Equals(SelectedSection?.Key, sectionKey, StringComparison.Ordinal);

    private static IReadOnlyList<SettingsSectionItem> BuildSections() =>
        new List<SettingsSectionItem>
        {
            new(SectionGeneral, "Allgemein", "Grunddaten"),
            new(SectionPaths, "Verzeichnisse", "Grunddaten"),
            new(SectionSystemMessage, "System-Nachricht", "Globale Prompts"),
            new(SectionAbstract, "Kurzfassung", "Globale Prompts"),
            new(SectionStructured, "Zusammenfassung", "Globale Prompts"),
            new(SectionProse, "Ausfuehrlich", "Globale Prompts"),
            new(SectionEmail, "E-Mail", "Typ-spezifische Prompts"),
            new(SectionAufgabe, "Aufgaben", "Typ-spezifische Prompts"),
            new(SectionGespraechsnotiz, "Gespraechsnotiz", "Typ-spezifische Prompts"),
            new(SectionStundenzettel, "Stundenzettel", "Typ-spezifische Prompts"),
            new(SectionAnalog, "Analog", "Typ-spezifische Prompts"),
        };

    private const string SectionGeneral = "general";
    private const string SectionPaths = "paths";
    private const string SectionSystemMessage = "system-message";
    private const string SectionAbstract = "abstract";
    private const string SectionStructured = "structured";
    private const string SectionProse = "prose";
    private const string SectionEmail = "email";
    private const string SectionAufgabe = "aufgabe";
    private const string SectionGespraechsnotiz = "gespraechsnotiz";
    private const string SectionStundenzettel = "stundenzettel";
    private const string SectionAnalog = "analog";
}

public sealed record SettingsSectionItem(string Key, string Label, string Group);
