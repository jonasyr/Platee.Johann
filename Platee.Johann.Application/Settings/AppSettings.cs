namespace Platee.Johann.Application.Settings;

using System;
using System.IO;
using Platee.Johann.Domain.ValueObjects;

/// <summary>
/// User-configurable application settings (personal preferences and paths).
/// Prompt-related settings have moved to <see cref="PromptSettings"/>.
/// </summary>
public sealed record AppSettings
{
    // User info
    public string Name { get; init; } = "Max Mustermann";

    public string Firma { get; init; } = "Musterfirma GmbH";

    // Directories
    public string Quellverzeichnis { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann", "Eingang");

    public string Archivverzeichnis { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann", "Eingang", "Archiv");

    public string Ausgabeverzeichnis { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann", "output");

    // Team / shared prompt file
    public string? GlobalPromptFilePath { get; init; } = @"Z:\12_Tools\Peano\Johann\prompts.json";

    // Release notes
    public string? LastSeenReleaseNotesVersion { get; init; }

    // Correction list for Whisper transcription fixes
    public IReadOnlyList<CorrectionEntry> Korrekturliste { get; init; } =
    [
        new() { Wrong = "Piano", Correct = "Peano" },
        new() { Wrong = "Nele", Correct = "Neele" },
        new() { Wrong = "JATJPT", Correct = "ChatGPT" },
        new() { Wrong = "JGPT", Correct = "ChatGPT" },
    ];

    /// <summary>
    /// Gets the per-section generation mode, keyed by built-in section id or category id.
    /// <para>
    /// Deliberately personal and stored in the local settings file: category definitions
    /// may be shared through the team prompts file, but nobody may change a colleague's
    /// waiting time. A missing key falls back to the SectionCatalog default.
    /// </para>
    /// </summary>
    public IReadOnlyDictionary<string, GenerationMode> SectionModes { get; init; }
        = new Dictionary<string, GenerationMode>();

    /// <summary>Gets a value indicating whether the one-time v1.4.0 mode prompt has been answered.</summary>
    public bool SectionModesMigrationDone { get; init; }

    /// <summary>
    /// Gets a value indicating whether the hint about sections that exist but were never
    /// generated has been dismissed for good.
    /// <para>
    /// Ticking such a section in the sidebar shows nothing, because the list controls
    /// visibility and not generation. Until #65 makes that visible in the entry itself,
    /// a one-off hint explains it and names the workaround.
    /// </para>
    /// </summary>
    public bool HideEmptySectionHint { get; init; }

    /// <summary>
    /// Gets die Id des Modells, das die Zusammenfassungen erzeugt.
    /// <para>
    /// Bewusst persoenlich und in der lokalen Einstellungsdatei: Prompts gehoeren dem Team,
    /// aber wie viel jemand fuer seine eigenen Zusammenfassungen ausgeben will, entscheidet
    /// er selbst. Eine Id, die <see cref="Processing.SummaryModelCatalog"/> nicht kennt, wird
    /// beim Start auf den Standard ueberstimmt statt hier repariert zu werden — sonst verloere
    /// der Nutzer seine Wahl still, sobald er einmal mit einer aelteren Version startet.
    /// </para>
    /// </summary>
    public string SummaryModel { get; init; } = Processing.ModelNames.Summaries;

    /// <summary>Gets a fresh instance with all default values.</summary>
    public static AppSettings Default => new();
}
