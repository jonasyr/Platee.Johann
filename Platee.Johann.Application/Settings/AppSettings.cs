namespace Platee.Johann.Application.Settings;

using System;
using System.IO;

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

    /// <summary>Gets a fresh instance with all default values.</summary>
    public static AppSettings Default => new();
}
