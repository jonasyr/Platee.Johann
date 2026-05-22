namespace Platee.Johann.Application.Settings;

using System;
using System.IO;
using Platee.Johann.Application.Processing;

/// <summary>
/// User-configurable application settings.
/// All prompt defaults mirror the original Python config.py constants.
/// </summary>
public sealed record AppSettings
{
    public int PromptDefaultsRevision { get; init; } = PromptDefaultsMigration.CurrentRevision;

    // User info
    public string Name { get; init; } = "Max Mustermann";

    public string Firma { get; init; } = "Musterfirma GmbH";

    // Directories
    public string Quellverzeichnis { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann", "Eingang");

    public string Archivverzeichnis { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann", "Eingang", "Archiv");

    public string Ausgabeverzeichnis { get; init; } = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann", "output");

    // General Prompts
    public string SystemMessage { get; init; } = SummaryPrompts.SystemMessage;

    public string AbstractPrompt { get; init; } = SummaryPrompts.Abstract;

    public string StructuredPrompt { get; init; } = SummaryPrompts.Structured;

    public string ProsePrompt { get; init; } = SummaryPrompts.Prose;

    // Type Prompts
    public string EmailPrompt { get; init; } = SummaryPrompts.Email;

    public string AufgabePrompt { get; init; } = SummaryPrompts.Aufgabe;

    public string GespraechsnotizPrompt { get; init; } = SummaryPrompts.Gespraechsnotiz;

    public string StundenzettelPrompt { get; init; } = SummaryPrompts.Stundenzettel;

    public string AnalogPrompt { get; init; } = SummaryPrompts.Analog;

    /// <summary>Gets a fresh instance with all default prompt values.</summary>
    public static AppSettings Default => new();
}
