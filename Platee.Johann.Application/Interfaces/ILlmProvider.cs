namespace Platee.Johann.Application.Interfaces;

/// <summary>
/// Optionen für einen einzelnen LLM-Aufruf.
/// <para>
/// <c>Model</c> ist angehängt und optional, damit alle vorhandenen Aufrufstellen und die
/// Test-Fakes unverändert weiterlaufen; <c>null</c> heißt „nimm den Standard des Providers“.
/// </para>
/// </summary>
/// <param name="MaxTokens">Obergrenze für die Ausgabe.</param>
/// <param name="UseReasoning">Ob das Modell ausdrücklich denken soll.</param>
/// <param name="Model">Die Modell-Id, oder <c>null</c> für den Standard.</param>
public sealed record LlmOptions(int MaxTokens = 20000, bool UseReasoning = false, string? Model = null);

public interface ILlmProvider
{
    bool IsAvailable { get; }

    Task<string> GenerateAsync(
        string systemPrompt,
        string userContent,
        LlmOptions options,
        CancellationToken ct = default);
}
