using Johann.Application.Interfaces;
using Johann.Application.Settings;

namespace Johann.Application.Processing;

/// <summary>
/// Generates GPT-based summaries for a transcript.
/// Mirrors generate_abstract / generate_long_summary / generate_prose_summary / generate_email_text
/// from Python summarizer.py.
/// Prompts are read from <see cref="SettingsHolder"/> so live edits propagate instantly.
/// </summary>
public sealed class SummaryGenerator
{
    private readonly ILlmProvider _llm;
    private readonly SettingsHolder _settings;

    public bool IsAvailable => _llm.IsAvailable;

    /// <summary>Convenience constructor for tests — uses default prompts.</summary>
    public SummaryGenerator(ILlmProvider llm)
        : this(llm, new SettingsHolder(AppSettings.Default)) { }

    public SummaryGenerator(ILlmProvider llm, SettingsHolder settings)
    {
        _llm      = llm;
        _settings = settings;
    }

    public async Task<string> GenerateAbstractAsync(string transcript, CancellationToken ct = default)
    {
        if (!_llm.IsAvailable || string.IsNullOrWhiteSpace(transcript))
            return string.Empty;

        var s = _settings.Current;
        var (abstractLimit, _) = WordLimitCalculator.Calculate(transcript);
        var userContent = s.AbstractPrompt
            .Replace("{word_limit}", abstractLimit.ToString())
            .Replace("{transcript}",  transcript);

        return await _llm.GenerateAsync(s.SystemMessage, userContent, new LlmOptions(20000), ct);
    }

    public async Task<string> GenerateLongSummaryAsync(string transcript, CancellationToken ct = default)
    {
        if (!_llm.IsAvailable || string.IsNullOrWhiteSpace(transcript))
            return string.Empty;

        var s = _settings.Current;
        var (_, structuredLimit) = WordLimitCalculator.Calculate(transcript);
        var userContent = s.StructuredPrompt
            .Replace("{word_limit}", structuredLimit.ToString())
            .Replace("{transcript}",  transcript);

        return await _llm.GenerateAsync(s.SystemMessage, userContent, new LlmOptions(20000), ct);
    }

    public async Task<string> GenerateProseSummaryAsync(string transcript, CancellationToken ct = default)
    {
        if (!_llm.IsAvailable || string.IsNullOrWhiteSpace(transcript))
            return string.Empty;

        var s = _settings.Current;
        var userContent = s.ProsePrompt
            .Replace("{transcript}", transcript);

        return await _llm.GenerateAsync(s.SystemMessage, userContent, new LlmOptions(20000), ct);
    }

    public async Task<string> GenerateEmailTextAsync(string proseSummary, CancellationToken ct = default)
    {
        if (!_llm.IsAvailable || string.IsNullOrWhiteSpace(proseSummary))
            return string.Empty;

        var s = _settings.Current;
        var userContent = s.EmailPrompt
            .Replace("{prose_summary}", proseSummary);

        return await _llm.GenerateAsync(s.SystemMessage, userContent, new LlmOptions(4000), ct);
    }
}
