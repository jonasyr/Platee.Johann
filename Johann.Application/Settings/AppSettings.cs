using Johann.Application.Processing;

namespace Johann.Application.Settings;

/// <summary>
/// User-configurable application settings.
/// All prompt defaults mirror the original Python config.py constants.
/// </summary>
public sealed record AppSettings
{
    public string SystemMessage    { get; init; } = SummaryPrompts.SystemMessage;
    public string AbstractPrompt   { get; init; } = SummaryPrompts.Abstract;
    public string StructuredPrompt { get; init; } = SummaryPrompts.Structured;
    public string ProsePrompt      { get; init; } = SummaryPrompts.Prose;
    public string EmailPrompt      { get; init; } = SummaryPrompts.Email;

    /// <summary>Returns a fresh instance with all default prompt values.</summary>
    public static AppSettings Default => new();
}
