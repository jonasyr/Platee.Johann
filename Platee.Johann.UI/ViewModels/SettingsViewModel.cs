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

    // ── Categories ───────────────────────────────────────────────────────────

    /// <summary>Gets the user-defined categories, in display order.</summary>
    public ObservableCollection<CategoryEditorViewModel> Categories { get; } = [];

    /// <summary>
    /// Gets the Auto / „Auf Knopfdruck“ toggles for the seven built-in sections.
    /// Custom categories carry their own toggle on their editor row.
    /// </summary>
    public IReadOnlyList<SectionModeRowViewModel> BuiltInSectionModes { get; }

    [ObservableProperty]
    private CategoryEditorViewModel? selectedCategory;

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

    public bool IsKategorienSelected => this.IsSelected(SectionKategorien);

    /// <summary>Gets a value indicating whether a category is selected for editing.</summary>
    public bool HasSelectedCategory => this.SelectedCategory is not null;

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
        this.BuiltInSectionModes = BuildBuiltInSectionModes(persistedHolder.Current.SectionModes);
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
        // A category added in this session still carries the placeholder id minted from
        // „Neue Kategorie". Re-mint it from the name the user actually typed, once, here —
        // before SectionModes are collected, because those are keyed by category id.
        this.FinalizeNewCategoryIds();

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

            // Modes are always personal, whatever SaveTarget says: category definitions may
            // be shared, but nobody may change a colleague's waiting time.
            SectionModes = this.CollectSectionModes(),
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
            CustomCategories = [.. this.Categories.Select(c => c.ToDefinition())],
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

    // ── Category commands ────────────────────────────────────────────────────

    /// <summary>
    /// Adds a category with a freshly minted, collision-free id.
    /// <para>
    /// Personal and on-demand by default: a new category must never silently slow down
    /// processing, and must never land in the team file without the user saying so.
    /// </para>
    /// </summary>
    [RelayCommand]
    private void AddCategory()
    {
        this.AppendCategory("Neue Kategorie", CategoryEditorViewModel.DefaultPrompt);
    }

    /// <summary>
    /// Copies a category's prompt into a new one. The copy gets its own id — sharing it
    /// would make both categories write to the same slot in <c>Entry.CustomSections</c>.
    /// </summary>
    [RelayCommand]
    private void DuplicateCategory(CategoryEditorViewModel? source)
    {
        var original = source ?? this.SelectedCategory;
        if (original is null)
        {
            return;
        }

        var copy = this.AppendCategory($"{original.Name} (Kopie)", original.Prompt);
        copy.Scope = original.Scope;
        copy.Mode = original.Mode;
    }

    [RelayCommand]
    private void RemoveCategory(CategoryEditorViewModel? category)
    {
        var target = category ?? this.SelectedCategory;
        if (target is null)
        {
            return;
        }

        this.Categories.Remove(target);
        this.RenumberCategories();
        this.SelectedCategory = this.Categories.FirstOrDefault();
    }

    [RelayCommand]
    private void MoveCategoryUp(CategoryEditorViewModel? category) =>
        this.MoveCategory(category, -1);

    [RelayCommand]
    private void MoveCategoryDown(CategoryEditorViewModel? category) =>
        this.MoveCategory(category, +1);

    /// <summary>
    /// Gives every not-yet-saved category its final id, derived from the name the user typed.
    /// <para>
    /// Minting at creation time is what produced <c>custom.neue-kategorie</c> for every first
    /// category: the id was derived from the placeholder name before the user had renamed it.
    /// </para>
    /// </summary>
    private void FinalizeNewCategoryIds()
    {
        foreach (var category in this.Categories.Where(c => c.HasProvisionalId).ToList())
        {
            var taken = this.Categories.Where(c => c != category).Select(c => c.Id);
            category.FinalizeId(CategoryIdFactory.Create(category.Name, taken));
        }
    }

    private CategoryEditorViewModel AppendCategory(string name, string prompt)
    {
        var id = CategoryIdFactory.Create(name, this.Categories.Select(c => c.Id));
        var editor = new CategoryEditorViewModel(
            new CategoryDefinition
            {
                Id = id,
                Name = name,
                Prompt = prompt,
                Scope = CategoryScope.Personal,
                Order = this.Categories.Count,
            },
            GenerationMode.OnDemand);

        editor.MarkProvisional();
        this.Categories.Add(editor);
        this.SelectedCategory = editor;
        return editor;
    }

    private void MoveCategory(CategoryEditorViewModel? category, int delta)
    {
        var target = category ?? this.SelectedCategory;
        if (target is null)
        {
            return;
        }

        var index = this.Categories.IndexOf(target);
        var newIndex = index + delta;
        if (index < 0 || newIndex < 0 || newIndex >= this.Categories.Count)
        {
            return;
        }

        this.Categories.Move(index, newIndex);
        this.RenumberCategories();
        this.SelectedCategory = target;
    }

    /// <summary>
    /// Rewrites <see cref="CategoryEditorViewModel.Order"/> from list position, so the
    /// persisted order matches what the user sees. Ids are untouched.
    /// </summary>
    private void RenumberCategories()
    {
        for (var i = 0; i < this.Categories.Count; i++)
        {
            this.Categories[i].Order = i;
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

        this.Categories.Clear();
        foreach (var category in p.CustomCategories.OrderBy(c => c.Order))
        {
            this.Categories.Add(new CategoryEditorViewModel(
                category,
                s.SectionModes.TryGetValue(category.Id, out var mode) ? mode : GenerationMode.OnDemand));
        }

        this.SelectedCategory = this.Categories.FirstOrDefault();
    }

    private static IReadOnlyList<SectionModeRowViewModel> BuildBuiltInSectionModes(
        IReadOnlyDictionary<string, GenerationMode> modes) =>
        [.. BuiltInSections.All.Select(id => new SectionModeRowViewModel(
            id,
            BuiltInSections.DisplayNameOf(id),
            modes.TryGetValue(id, out var mode) ? mode : GenerationMode.Auto))];

    /// <summary>
    /// Collects the per-section modes from both toggle sources — the built-in rows and each
    /// category's own toggle — into the single map persisted in the local settings file.
    /// </summary>
    private Dictionary<string, GenerationMode> CollectSectionModes()
    {
        var modes = new Dictionary<string, GenerationMode>(StringComparer.Ordinal);
        foreach (var row in this.BuiltInSectionModes)
        {
            modes[row.Id] = row.Mode;
        }

        foreach (var category in this.Categories)
        {
            modes[category.Id] = category.Mode;
        }

        return modes;
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
        OnPropertyChanged(nameof(IsKategorienSelected));
    }

    partial void OnSelectedCategoryChanged(CategoryEditorViewModel? value) =>
        this.OnPropertyChanged(nameof(this.HasSelectedCategory));

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
            new(SectionKategorien, "Kategorien", "GRUNDDATEN"),
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
    private const string SectionKategorien = "kategorien";
}

public sealed record SettingsSectionItem(string Key, string Label, string Group);
