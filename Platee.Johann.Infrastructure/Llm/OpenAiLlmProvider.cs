namespace Platee.Johann.Infrastructure.Llm;

using OpenAI.Chat;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;

/// <summary>
/// OpenAI chat completion provider, used for every generated section.
/// Runs on <see cref="ModelNames.Summaries"/> with max_completion_tokens.
/// </summary>
public sealed class OpenAiLlmProvider : ILlmProvider
{
    /// <summary>
    /// The model that writes the summaries.
    /// <para>
    /// OpenAI positions Luna explicitly for "summarization, drafting, classification",
    /// which is exactly this application's job. It costs 0.20/1.20 $ per 1M tokens against
    /// 0.05/0.40 for the previous <c>gpt-5-nano</c> — four times the token price, but #61
    /// cuts a dictation from eight calls to two, so the change is roughly cost-neutral for
    /// a markedly better result.
    /// </para>
    /// <para>
    /// Public so the choice is covered by a test: a silently reverted model would not fail
    /// anything, the app would just get worse and more expensive. #71 will make this
    /// user-selectable; until then it is the single fixed default.
    /// </para>
    /// </summary>
    public const string ModelName = ModelNames.Summaries;

    // Deliberately not IDisposable. ChatClient (OpenAI 2.2.0) implements no
    // interfaces at all, so casting it to IDisposable is always null — an earlier
    // attempt to dispose it that way was a guaranteed no-op. The SDK's
    // System.ClientModel pipeline owns its transport, and exactly one provider is
    // created for the process lifetime, so there is nothing here to release.
    private readonly ChatClient client;

    public bool IsAvailable => true;

    public OpenAiLlmProvider(string apiKey)
    {
        this.client = new ChatClient(ModelName, apiKey);
    }

    public async Task<string> GenerateAsync(
        string systemPrompt,
        string userContent,
        LlmOptions options,
        CancellationToken ct = default)
    {
        ChatMessage[] messages =
        [
            new SystemChatMessage(systemPrompt),
            new UserChatMessage(userContent),
        ];

        var chatOptions = new ChatCompletionOptions
        {
            MaxOutputTokenCount = options.MaxTokens,
        };

        var response = await this.client.CompleteChatAsync(messages, chatOptions, ct);
        return response.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
    }
}
