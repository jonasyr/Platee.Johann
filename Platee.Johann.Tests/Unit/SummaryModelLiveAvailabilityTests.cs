namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Infrastructure.Llm;
using Xunit;

/// <summary>
/// Prueft, dass jedes Modell im Katalog bei OpenAI wirklich existiert.
/// <para>
/// Faengt genau den Fehler ab, vor dem #71 warnt: OpenAI benennt ein Modell um oder kuendigt
/// es ab, und der Katalog bietet weiter etwas Totes an. Dann scheitert beim Nutzer jedes
/// Diktat, und niemand weiss warum.
/// </para>
/// <para>
/// Braucht einen echten Schluessel und laeuft deshalb auf CI nie — dort wird der Test als
/// <b>uebersprungen</b> ausgewiesen und nicht still gruen. Kostet vier Aufrufe mit
/// minimaler Ausgabe, also Bruchteile eines Cents.
/// </para>
/// </summary>
public sealed class SummaryModelLiveAvailabilityTests
{
    private static string? ApiKey => ApiKeyProvider.TryGetOpenAiKey();

    public static TheoryData<string> CatalogIds()
    {
        var data = new TheoryData<string>();
        foreach (var model in SummaryModelCatalog.All)
        {
            data.Add(model.Id);
        }

        return data;
    }

    [SkippableTheory]
    [MemberData(nameof(CatalogIds))]
    public async Task Every_catalog_model_still_exists(string modelId)
    {
        Skip.If(ApiKey is null, "Kein OPENAI_API_KEY gefunden — Live-Pruefung uebersprungen.");

        var provider = new OpenAiLlmProvider(ApiKey!);

        var call = async () => await provider.GenerateAsync(
            systemPrompt: "Antworte mit genau einem Wort.",
            userContent: "Sag: ok",
            options: new LlmOptions(MaxTokens: 16, Model: modelId));

        // Eine abgekuendigte oder umbenannte Id beantwortet OpenAI mit HTTP 404, das SDK
        // wirft daraufhin — genau darauf zielt dieser Test.
        //
        // Der *Text* wird bewusst nicht geprueft. Alle vier sind Reasoning-Modelle: bei
        // gpt-5-nano gingen selbst bei einem "Sag: ok" in drei von vier Messungen 256+
        // Token ins interne Denken, finish_reason war "length" und der sichtbare Inhalt
        // leer. Eine Textzusicherung wuerde also das Denkverhalten messen statt der
        // Existenz und den Test grundlos flackern lassen.
        await call.Should().NotThrowAsync();
    }
}
