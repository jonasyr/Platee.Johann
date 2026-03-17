namespace Platee.Johann.Application.Interfaces;

public sealed record LlmOptions(int MaxTokens = 20000, bool UseReasoning = false);

public interface ILlmProvider
{
    bool IsAvailable { get; }

    Task<string> GenerateAsync(
        string systemPrompt,
        string userContent,
        LlmOptions options,
        CancellationToken ct = default);
}
