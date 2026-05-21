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
