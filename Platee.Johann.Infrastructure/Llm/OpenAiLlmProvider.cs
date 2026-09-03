namespace Platee.Johann.Infrastructure.Llm;

using OpenAI.Chat;
using Platee.Johann.Application.Interfaces;

/// <summary>
/// OpenAI ChatGPT provider.  Uses gpt-5-nano with max_completion_tokens.
/// Mirrors _call_gpt() from Python summarizer.py.
/// </summary>
public sealed class OpenAiLlmProvider : ILlmProvider
{
    private const string Model = "gpt-5-nano";

    // Deliberately not IDisposable. ChatClient (OpenAI 2.2.0) implements no
    // interfaces at all, so casting it to IDisposable is always null — an earlier
    // attempt to dispose it that way was a guaranteed no-op. The SDK's
    // System.ClientModel pipeline owns its transport, and exactly one provider is
    // created for the process lifetime, so there is nothing here to release.
    private readonly ChatClient client;

    public bool IsAvailable => true;

    public OpenAiLlmProvider(string apiKey)
    {
        this.client = new ChatClient(Model, apiKey);
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
