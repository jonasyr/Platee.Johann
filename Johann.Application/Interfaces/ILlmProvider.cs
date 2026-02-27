namespace Johann.Application.Interfaces;

public sealed record LlmOptions(int MaxTokens = 2000, bool UseReasoning = false);

public interface ILlmProvider
{
    bool IsAvailable { get; }
    Task<string> GenerateAsync(string prompt, LlmOptions options, CancellationToken ct = default);
}
