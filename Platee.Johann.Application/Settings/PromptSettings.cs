namespace Platee.Johann.Application.Settings;

using Platee.Johann.Application.Processing;

public sealed record PromptSettings
{
    public int PromptDefaultsRevision { get; init; } = PromptDefaultsMigration.CurrentRevision;

    public string SystemMessage { get; init; } = SummaryPrompts.SystemMessage;

    public string AbstractPrompt { get; init; } = SummaryPrompts.Abstract;

    public string StructuredPrompt { get; init; } = SummaryPrompts.Structured;

    public string ProsePrompt { get; init; } = SummaryPrompts.Prose;

    public string EmailPrompt { get; init; } = SummaryPrompts.Email;

    public string AufgabePrompt { get; init; } = SummaryPrompts.Aufgabe;

    public string GespraechsnotizPrompt { get; init; } = SummaryPrompts.Gespraechsnotiz;

    public string StundenzettelPrompt { get; init; } = SummaryPrompts.Stundenzettel;

    public string AnalogPrompt { get; init; } = SummaryPrompts.Analog;

    public static PromptSettings Default => new();
}
