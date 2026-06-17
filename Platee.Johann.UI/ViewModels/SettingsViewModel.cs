namespace Platee.Johann.UI.ViewModels;

using System;
using System.Collections.Generic;
using System.IO;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.Infrastructure.Json;

public sealed partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsRepository repository;
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

    [ObservableProperty]
    private string statusMessage = string.Empty;
    [ObservableProperty]
    private string pathStatusMessage = string.Empty;
    [ObservableProperty]
    private SettingsSectionItem? selectedSection;

    [ObservableProperty]
    private bool isAdminMode;

    [ObservableProperty]
    private string adminButtonLabel = "Admin";

    [ObservableProperty]
    private string promptWarningText = DefaultPromptWarning;

    private const string DefaultPromptWarning =
        "Hinweis: Änderungen an Prompts gelten nur temporär bis zum nächsten App-Neustart und nur für Sie persönlich. Nach dem Neustart werden die globalen Team-Prompts wiederhergestellt. Für dauerhafte Änderungen bitte mit US/JW in Verbindung setzen.";

    private const string AdminPromptWarning =
        "ACHTUNG: Sie bearbeiten die globalen Team-Prompts. Änderungen betreffen ALLE Mitarbeiter nach deren nächstem App-Neustart!";

    public IReadOnlyList<SettingsSectionItem> Sections { get; }

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

    public bool HasPathStatusMessage => !string.IsNullOrWhiteSpace(this.PathStatusMessage);

    public bool IsPromptReadOnly => !this.IsAdminMode;

    /// <summary>
    /// Delegate that shows the admin password dialog and returns the entered password,
    /// or null if the dialog was cancelled. Set by the UI layer; null-safe in tests.
    /// </summary>
    public Func<string?>? ShowAdminPasswordDialog { get; set; }

    public SettingsViewModel(
        ISettingsRepository repository,
        SettingsHolder persistedHolder,
        SettingsHolder? runtimeHolder = null,
        IReadOnlyList<StartupPathIssue>? startupPathIssues = null)
    {
        this.repository = repository;
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

    public void ActivateAdmin(string password)
    {
        if (password == "123")
        {
            this.IsAdminMode = true;
        }
    }

    public void DeactivateAdmin()
    {
        this.IsAdminMode = false;
    }

    [RelayCommand]
    private void ToggleAdmin()
    {
        if (this.IsAdminMode)
        {
            this.DeactivateAdmin();
            return;
        }

        var password = this.ShowAdminPasswordDialog?.Invoke();
        if (password is not null)
        {
            this.ActivateAdmin(password);
        }
    }

    partial void OnIsAdminModeChanged(bool value)
    {
        this.AdminButtonLabel = value ? "Admin aktiv" : "Admin";
        this.PromptWarningText = value ? AdminPromptWarning : DefaultPromptWarning;
        this.OnPropertyChanged(nameof(this.IsPromptReadOnly));
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

        // Persist only personal settings — prompts are never saved locally
        await this.repository.SaveAsync(updatedSettings);

        this.persistedHolder.Current = updatedSettings;
        this.runtimeHolder.Current = updatedSettings;

        // Prompts
        var promptsChanged = updatedPrompts != this.persistedHolder.Prompts;

        if (this.IsAdminMode && promptsChanged)
        {
            // Admin mode: persist prompts to the global team file
            var globalPath = this.persistedHolder.Current.GlobalPromptFilePath
                             ?? updatedSettings.GlobalPromptFilePath;
            if (!string.IsNullOrWhiteSpace(globalPath))
            {
                var globalRepo = JsonPromptSettingsRepository.FromFilePath(globalPath);
                await globalRepo.SaveAsync(updatedPrompts);
                this.persistedHolder.Prompts = updatedPrompts;
                this.runtimeHolder.Prompts = updatedPrompts;
                this.StatusMessage = "✓ Globale Prompts für alle Mitarbeiter gespeichert.";
            }
        }
        else if (promptsChanged)
        {
            // Normal mode: session-only
            this.runtimeHolder.Prompts = updatedPrompts;
            this.StatusMessage = "✓ Einstellungen gespeichert. Prompt-Änderungen gelten nur bis zum nächsten Neustart.";
        }
        else
        {
            this.StatusMessage = "✓ Einstellungen gespeichert.";
        }

        this.PathStatusMessage = string.Empty;
        this.OnPropertyChanged(nameof(this.HasPathStatusMessage));
    }

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
}

public sealed record SettingsSectionItem(string Key, string Label, string Group);
