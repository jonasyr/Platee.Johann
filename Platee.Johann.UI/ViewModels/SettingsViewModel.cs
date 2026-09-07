namespace Platee.Johann.UI.ViewModels;

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.Infrastructure.Json;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository repository;
    private readonly IPromptSettingsRepository promptRepository;
    private readonly SettingsHolder persistedHolder;
    private readonly SettingsHolder runtimeHolder;

    // ── User info ─────────────────────────────────────────────────────────────
    [ObservableProperty]
    private string name = string.Empty;
    [ObservableProperty]
    private string firma = string.Empty;

    // ── Directories ───────────────────────────────────────────────────────────
    [ObservableProperty]
    private string quellverzeichnis = string.Empty;
    [ObservableProperty]
    private string archivverzeichnis = string.Empty;
    [ObservableProperty]
    private string ausgabeverzeichnis = string.Empty;

    // ── Team / global prompt ──────────────────────────────────────────────────
    [ObservableProperty]
    private string? globalPromptFilePath;
    [ObservableProperty]
    private string globalPromptStatus = string.Empty;

    // ── General prompts ───────────────────────────────────────────────────────
    [ObservableProperty]
    private string systemMessage = string.Empty;
    [ObservableProperty]
    private string abstractPrompt = string.Empty;
    [ObservableProperty]
    private string structuredPrompt = string.Empty;
    [ObservableProperty]
    private string prosePrompt = string.Empty;

    // ── Type-specific prompts ─────────────────────────────────────────────────
    [ObservableProperty]
    private string emailPrompt = string.Empty;
    [ObservableProperty]
    private string aufgabePrompt = string.Empty;
    [ObservableProperty]
    private string gespraechsnotizPrompt = string.Empty;
    [ObservableProperty]
    private string stundenzettelPrompt = string.Empty;
    [ObservableProperty]
    private string analogPrompt = string.Empty;

    // ── Correction list ──────────────────────────────────────────────────────
    public ObservableCollection<CorrectionEntryViewModel> Korrekturen { get; } = [];

    [ObservableProperty]
    private string statusMessage = string.Empty;
    [ObservableProperty]
    private string pathStatusMessage = string.Empty;
    [ObservableProperty]
    private SettingsSectionItem? selectedSection;

    /// <summary>
    /// Gets or sets where a prompt or category edit is written. Replaces the former admin
    /// password gate, which doubled as an implicit — and invisible — write-target switch.
    /// </summary>
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsGlobalTarget))]
    [NotifyPropertyChangedFor(nameof(PromptWarningText))]
    private CategoryScope saveTarget = CategoryScope.Personal;

    public IReadOnlyList<SettingsSectionItem> Sections { get; }

    /// <summary>Gets a value indicating whether edits are written to the shared team file.</summary>
    public bool IsGlobalTarget => this.SaveTarget == CategoryScope.Global;

    /// <summary>Gets the warning shown above every prompt editor, driven by the save target.</summary>
    public string PromptWarningText => this.IsGlobalTarget
        ? "⚠ Ziel „Global (Team)“: Diese Änderung wirkt für alle Nutzer nach deren nächstem App-Neustart."
        : "Ziel „Persönlich“: Diese Änderung gilt nur für Sie und wird lokal gespeichert.";

    public bool IsGeneralSelected => this.IsSelected(SectionGeneral);

    public bool IsPathsSelected => this.IsSelected(SectionPaths);

    public bool IsTeamSelected => this.IsSelected(SectionTeam);

    public bool IsSystemMessageSelected => this.IsSelected(SectionSystemMessage);

    public bool IsAbstractSelected => this.IsSelected(SectionAbstract);

    public bool IsStructuredSelected => this.IsSelected(SectionStructured);

    public bool IsProseSelected => this.IsSelected(SectionProse);

    public bool IsEmailSelected => this.IsSelected(SectionEmail);

    public bool IsAufgabeSelected => this.IsSelected(SectionAufgabe);

    public bool IsGespraechsnotizSelected => this.IsSelected(SectionGespraechsnotiz);

    public bool IsStundenzettelSelected => this.IsSelected(SectionStundenzettel);

    public bool IsAnalogSelected => this.IsSelected(SectionAnalog);

    public bool IsKorrekturlisteSelected => this.IsSelected(SectionKorrekturliste);

    public bool HasPathStatusMessage => !string.IsNullOrWhiteSpace(this.PathStatusMessage);

    public SettingsViewModel(
        ISettingsRepository repository,
        IPromptSettingsRepository promptRepository,
        SettingsHolder persistedHolder,
        SettingsHolder? runtimeHolder = null,
        IReadOnlyList<StartupPathIssue>? startupPathIssues = null)
    {
        this.repository = repository;
        this.promptRepository = promptRepository;
        this.persistedHolder = persistedHolder;
        this.runtimeHolder = runtimeHolder ?? persistedHolder;
        this.Sections = BuildSections();
        this.LoadFromHolder();
        if (startupPathIssues is { Count: > 0 })
        {
            this.PathStatusMessage = BuildPathStatusMessage(startupPathIssues);
        }

        this.SelectedSection = this.Sections[0];
    }

    [RelayCommand]
    private async Task SaveAsync()
    {
        var updatedSettings = this.persistedHolder.Current with
        {
            Name = this.Name.Trim(),
            Firma = this.Firma.Trim(),
            Quellverzeichnis = this.Quellverzeichnis.Trim(),
            Archivverzeichnis = this.Archivverzeichnis.Trim(),
            Ausgabeverzeichnis = this.Ausgabeverzeichnis.Trim(),
            GlobalPromptFilePath = string.IsNullOrWhiteSpace(this.GlobalPromptFilePath) ? null : this.GlobalPromptFilePath.Trim(),
            Korrekturliste = this.Korrekturen
                .Where(c => !string.IsNullOrWhiteSpace(c.Wrong))
                .Select(c => new CorrectionEntry { Wrong = c.Wrong.Trim(), Correct = c.Correct.Trim() })
                .ToList(),
        };

        var updatedPrompts = this.runtimeHolder.Prompts with
        {
            SystemMessage = this.SystemMessage.Trim(),
            AbstractPrompt = this.AbstractPrompt.Trim(),
            StructuredPrompt = this.StructuredPrompt.Trim(),
            ProsePrompt = this.ProsePrompt.Trim(),
            EmailPrompt = this.EmailPrompt.Trim(),
            AufgabePrompt = this.AufgabePrompt.Trim(),
            GespraechsnotizPrompt = this.GespraechsnotizPrompt.Trim(),
            StundenzettelPrompt = this.StundenzettelPrompt.Trim(),
            AnalogPrompt = this.AnalogPrompt.Trim(),
        };

        await this.repository.SaveAsync(updatedSettings);

        this.persistedHolder.Update(updatedSettings, this.persistedHolder.Prompts);
        this.runtimeHolder.Update(updatedSettings, this.runtimeHolder.Prompts);

        await this.SavePromptsAsync(updatedPrompts, updatedSettings);

        this.PathStatusMessage = string.Empty;
        this.OnPropertyChanged(nameof(this.HasPathStatusMessage));
    }

    /// <summary>
    /// Writes the prompts to whichever file <see cref="SaveTarget"/> selects. Before v1.4.0
    /// this decision hung off the admin password: on meant "global file", off meant
    /// "session-only", so every non-admin prompt edit was silently lost on restart.
    /// </summary>
    private async Task SavePromptsAsync(PromptSettings updatedPrompts, AppSettings updatedSettings)
    {
        if (!HasPromptChanges(updatedPrompts, this.persistedHolder.Prompts))
        {
            this.StatusMessage = "✓ Einstellungen gespeichert.";
            return;
        }

        if (!this.IsGlobalTarget)
        {
            await this.SavePersonalPromptsAsync(updatedPrompts);
            this.StatusMessage = "✓ Persönliche Prompts gespeichert.";
            return;
        }

        var globalPath = this.persistedHolder.Current.GlobalPromptFilePath
                         ?? updatedSettings.GlobalPromptFilePath;
        if (string.IsNullOrWhiteSpace(globalPath))
        {
            this.StatusMessage = "⚠ Kein globaler Prompt-Pfad konfiguriert – bitte unter „Team-Prompts“ eintragen.";
            return;
        }

        try
        {
            var globalRepo = JsonPromptSettingsRepository.FromFilePath(globalPath);
            await globalRepo.SaveAsync(updatedPrompts);
            this.persistedHolder.Update(this.persistedHolder.Current, updatedPrompts);
            this.runtimeHolder.Update(this.runtimeHolder.Current, updatedPrompts);
            this.StatusMessage = "✓ Globale Prompts für alle Mitarbeiter gespeichert.";
        }
        catch (Exception ex)
        {
            // A read-only or unreachable share must not swallow the edit (#45, #50).
            //
            // What can actually be rescued differs by kind, and the message must not
            // overstate it: the user's own categories belong in the personal file and are
            // saved there, but the eight built-in prompts are owned by the team file alone
            // — writing them locally would shadow the team baseline for this user forever.
            // So prompt text survives only for this session, and we say exactly that.
            var hadPersonalCategories = updatedPrompts.CustomCategories
                .Any(c => c.Scope == CategoryScope.Personal);

            await this.SavePersonalPromptsAsync(updatedPrompts);
            this.SaveTarget = CategoryScope.Personal;

            var rescued = hadPersonalCategories
                ? "Eigene Kategorien wurden persönlich gespeichert; Prompt-Änderungen"
                : "Prompt-Änderungen";

            this.StatusMessage =
                $"⚠ Globale Datei nicht schreibbar ({ex.Message}). "
                + $"{rescued} gelten nur bis zum nächsten Neustart.";
        }
    }

    /// <summary>
    /// Writes the user's own categories to the local personal file.
    /// <para>
    /// Only the categories are persisted, never the eight built-in prompts: the team file
    /// on the share stays their sole owner, so a personal file can never shadow a team
    /// prompt and freeze its owner out of baseline updates (#45 H1). Storing the team's
    /// prompt text here as well would also leave a stale copy that silently diverges the
    /// moment the team file changes.
    /// </para>
    /// </summary>
    private async Task SavePersonalPromptsAsync(PromptSettings updatedPrompts)
    {
        var personalOnly = PromptSettings.Default with
        {
            CustomCategories = [.. updatedPrompts.CustomCategories
                .Where(c => c.Scope == CategoryScope.Personal)],
        };

        await this.promptRepository.SaveAsync(personalOnly);
        this.persistedHolder.Update(this.persistedHolder.Current, updatedPrompts);
        this.runtimeHolder.Update(this.runtimeHolder.Current, updatedPrompts);
    }

    /// <summary>
    /// Compares two prompt sets by content. The record's own <c>!=</c> is not usable:
    /// <see cref="PromptSettings.CustomCategories"/> is an <see cref="IReadOnlyList{T}"/>,
    /// which compares by reference, so a plain record comparison reports a change on
    /// every single save — and would write to the shared team file each time.
    /// </summary>
    private static bool HasPromptChanges(PromptSettings updated, PromptSettings current) =>
        (updated with { CustomCategories = [] }) != (current with { CustomCategories = [] })
        || !updated.CustomCategories.SequenceEqual(current.CustomCategories);

    [RelayCommand]
    private void Reset()
    {
        var d = AppSettings.Default;
        this.Name = d.Name;
        this.Firma = d.Firma;
        this.Quellverzeichnis = d.Quellverzeichnis;
        this.Archivverzeichnis = d.Archivverzeichnis;
        this.Ausgabeverzeichnis = d.Ausgabeverzeichnis;
        this.GlobalPromptFilePath = d.GlobalPromptFilePath;

        // Reload prompts from what was loaded at startup (global file or defaults)
        var p = this.persistedHolder.Prompts;
        this.SystemMessage = p.SystemMessage;
        this.AbstractPrompt = p.AbstractPrompt;
        this.StructuredPrompt = p.StructuredPrompt;
        this.ProsePrompt = p.ProsePrompt;
        this.EmailPrompt = p.EmailPrompt;
        this.AufgabePrompt = p.AufgabePrompt;
        this.GespraechsnotizPrompt = p.GespraechsnotizPrompt;
        this.StundenzettelPrompt = p.StundenzettelPrompt;
        this.AnalogPrompt = p.AnalogPrompt;
        this.Korrekturen.Clear();
        foreach (var c in d.Korrekturliste)
        {
            this.Korrekturen.Add(new CorrectionEntryViewModel { Wrong = c.Wrong, Correct = c.Correct });
        }

        this.StatusMessage = "Werte zurückgesetzt – noch nicht gespeichert.";
    }

    [RelayCommand]
    private void BrowseQuell()
    {
        var path = PickFolder(this.Quellverzeichnis);
        if (path is not null)
        {
            this.Quellverzeichnis = path;
        }
    }

    [RelayCommand]
    private void BrowseArchiv()
    {
        var path = PickFolder(this.Archivverzeichnis);
        if (path is not null)
        {
            this.Archivverzeichnis = path;
        }
    }

    [RelayCommand]
    private void BrowseAusgabe()
    {
        var path = PickFolder(this.Ausgabeverzeichnis);
        if (path is not null)
        {
            this.Ausgabeverzeichnis = path;
        }
    }

    [RelayCommand]
    private void AddCorrection()
    {
        this.Korrekturen.Add(new CorrectionEntryViewModel());
    }

    [RelayCommand]
    private void RemoveCorrection(CorrectionEntryViewModel? entry)
    {
        if (entry is not null)
        {
            this.Korrekturen.Remove(entry);
        }
    }

    // ── Private ───────────────────────────────────────────────────────────────
    private void LoadFromHolder()
    {
        var s = this.persistedHolder.Current;
        this.Name = s.Name;
        this.Firma = s.Firma;
        this.Quellverzeichnis = s.Quellverzeichnis;
        this.Archivverzeichnis = s.Archivverzeichnis;
        this.Ausgabeverzeichnis = s.Ausgabeverzeichnis;
        this.GlobalPromptFilePath = s.GlobalPromptFilePath;
        this.GlobalPromptStatus = EvaluateGlobalPromptStatus(s.GlobalPromptFilePath);

        var p = this.persistedHolder.Prompts;
        this.SystemMessage = p.SystemMessage;
        this.AbstractPrompt = p.AbstractPrompt;
        this.StructuredPrompt = p.StructuredPrompt;
        this.ProsePrompt = p.ProsePrompt;
        this.EmailPrompt = p.EmailPrompt;
        this.AufgabePrompt = p.AufgabePrompt;
        this.GespraechsnotizPrompt = p.GespraechsnotizPrompt;
        this.StundenzettelPrompt = p.StundenzettelPrompt;
        this.AnalogPrompt = p.AnalogPrompt;

        this.Korrekturen.Clear();
        foreach (var c in s.Korrekturliste)
        {
            this.Korrekturen.Add(new CorrectionEntryViewModel { Wrong = c.Wrong, Correct = c.Correct });
        }
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
        OnPropertyChanged(nameof(HasPathStatusMessage));
        OnPropertyChanged(nameof(IsGeneralSelected));
        OnPropertyChanged(nameof(IsPathsSelected));
        OnPropertyChanged(nameof(IsTeamSelected));
        OnPropertyChanged(nameof(IsSystemMessageSelected));
        OnPropertyChanged(nameof(IsAbstractSelected));
        OnPropertyChanged(nameof(IsStructuredSelected));
        OnPropertyChanged(nameof(IsProseSelected));
        OnPropertyChanged(nameof(IsEmailSelected));
        OnPropertyChanged(nameof(IsAufgabeSelected));
        OnPropertyChanged(nameof(IsGespraechsnotizSelected));
        OnPropertyChanged(nameof(IsStundenzettelSelected));
        OnPropertyChanged(nameof(IsAnalogSelected));
        OnPropertyChanged(nameof(IsKorrekturlisteSelected));
    }

    private bool IsSelected(string sectionKey) =>
        string.Equals(this.SelectedSection?.Key, sectionKey, StringComparison.Ordinal);

    private static string BuildPathStatusMessage(IReadOnlyList<StartupPathIssue> issues)
    {
        var labels = string.Join(", ", issues.Select(x => x.Label));
        return $"Hinweis: Die hier angezeigten Pfade sind die gespeicherten Werte. Beim letzten Start wurden für diese Sitzung Ersatzpfade verwendet ({labels}). Bitte bei Bedarf korrigieren und speichern.";
    }

    [RelayCommand]
    private void BrowseGlobalPromptFile()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Globale Prompt-Datei auswählen",
            Filter = "JSON-Dateien (*.json)|*.json|Alle Dateien (*.*)|*.*",
            FileName = "prompts.json",
        };

        if (dialog.ShowDialog() == true)
        {
            this.GlobalPromptFilePath = dialog.FileName;
        }
    }

    private static IReadOnlyList<SettingsSectionItem> BuildSections() =>
        new List<SettingsSectionItem>
        {
            new(SectionGeneral, "Allgemein", "GRUNDDATEN"),
            new(SectionPaths, "Verzeichnisse", "GRUNDDATEN"),
            new(SectionTeam, "Team-Prompts", "GRUNDDATEN"),
            new(SectionKorrekturliste, "Korrekturliste", "GRUNDDATEN"),
            new(SectionSystemMessage, "System-Nachricht", "GLOBALE PROMPTS"),
            new(SectionAbstract, "Kurzfassung", "GLOBALE PROMPTS"),
            new(SectionStructured, "Zusammenfassung", "GLOBALE PROMPTS"),
            new(SectionProse, "Ausfuehrlich", "GLOBALE PROMPTS"),
            new(SectionEmail, "E-Mail", "TYP-SPEZIFISCHE PROMPTS"),
            new(SectionAufgabe, "Aufgaben", "TYP-SPEZIFISCHE PROMPTS"),
            new(SectionGespraechsnotiz, "Gespraechsnotiz", "TYP-SPEZIFISCHE PROMPTS"),
            new(SectionStundenzettel, "Stundenzettel", "TYP-SPEZIFISCHE PROMPTS"),
            new(SectionAnalog, "Analog", "TYP-SPEZIFISCHE PROMPTS"),
        };

    private static string EvaluateGlobalPromptStatus(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return string.Empty;
        }

        if (File.Exists(path))
        {
            return "✓ Globale Prompt-Datei erreichbar";
        }

        return "⚠ Globale Prompt-Datei nicht erreichbar – lokale Prompts werden verwendet";
    }

    partial void OnGlobalPromptFilePathChanged(string? value)
    {
        this.GlobalPromptStatus = EvaluateGlobalPromptStatus(value);
    }

    private const string SectionGeneral = "general";
    private const string SectionPaths = "paths";
    private const string SectionTeam = "team";
    private const string SectionSystemMessage = "system-message";
    private const string SectionAbstract = "abstract";
    private const string SectionStructured = "structured";
    private const string SectionProse = "prose";
    private const string SectionEmail = "email";
    private const string SectionAufgabe = "aufgabe";
    private const string SectionGespraechsnotiz = "gespraechsnotiz";
    private const string SectionStundenzettel = "stundenzettel";
    private const string SectionAnalog = "analog";
    private const string SectionKorrekturliste = "korrekturliste";
}

public sealed record SettingsSectionItem(string Key, string Label, string Group);
