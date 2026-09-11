namespace Platee.Johann.Infrastructure.Llm;

using System.Collections.Concurrent;
using OpenAI.Chat;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;

/// <summary>
/// OpenAI chat completion provider, used for every generated section.
/// <para>
/// Das Modell kommt seit #71 je Aufruf aus <see cref="LlmOptions.Model"/> — der Nutzer
/// waehlt es in den Einstellungen. Ohne Angabe gilt <see cref="SummaryModelCatalog.Default"/>.
/// </para>
/// </summary>
public sealed class OpenAiLlmProvider : ILlmProvider
{
    // Deliberately not IDisposable. ChatClient (OpenAI 2.2.0) implements no
    // interfaces at all, so casting it to IDisposable is always null — an earlier
    // attempt to dispose it that way was a guaranteed no-op. The SDK's
    // System.ClientModel pipeline owns its transport, and the handful of clients
    // here live for the process, so there is nothing to release.
    //
    // Je Modell-Id ein Client: ChatClient bindet das Modell im Konstruktor, ein Wechsel
    // zur Laufzeit braucht also einen zweiten. Der Katalog hat vier Eintraege, die Map
    // wird nicht gross.
    private readonly ConcurrentDictionary<string, ChatClient> clients = new(StringComparer.Ordinal);

    private readonly string apiKey;

    public OpenAiLlmProvider(string apiKey)
    {
        this.apiKey = apiKey;
    }

    public bool IsAvailable => true;

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

        var client = this.ResolveClient(options.Model);

        var response = await client.CompleteChatAsync(messages, chatOptions, ct);
        return response.Value.Content.FirstOrDefault()?.Text ?? string.Empty;
    }

    /// <summary>
    /// Liefert den Client für die Modell-Id und legt ihn beim ersten Mal an.
    /// </summary>
    /// <param name="model">Die gewünschte Modell-Id, oder <c>null</c> für den Standard.</param>
    /// <returns>Ein auf dieses Modell gebundener <see cref="ChatClient"/>.</returns>
    private ChatClient ResolveClient(string? model) =>
        this.clients.GetOrAdd(
            model ?? SummaryModelCatalog.Default.Id,
            id => new ChatClient(id, this.apiKey));
}
