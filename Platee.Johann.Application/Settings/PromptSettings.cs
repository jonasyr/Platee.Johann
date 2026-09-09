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

    /// <summary>
    /// Gets the user-defined categories. Present in both the global and the local prompts
    /// file; <c>PromptSettingsLoader.MergeCategories</c> combines them, with a personal
    /// category winning over a global one sharing its Id.
    /// </summary>
    public IReadOnlyList<CategoryDefinition> CustomCategories { get; init; } = [];

    public static PromptSettings Default => new();
}
