namespace Platee.Johann.Tests.Unit;

using System.ClientModel;
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
/// <b>uebersprungen</b> ausgewiesen und nicht still gruen. Kostet je Katalogmodell einen
/// Aufruf mit kleiner Ausgabe, also Bruchteile eines Cents.
/// </para>
/// </summary>
public sealed class SummaryModelLiveAvailabilityTests
{
    // Alle Katalogmodelle sind Reasoning-Modelle. Mit 16 Token ging das Limit gelegentlich
    // ganz ins Denken (#84); 256 reichen fuer Denken plus ein Wort.
    private const int ProbeMaxTokens = 256;

    private const string InventedModelId = "gpt-johann-gibt-es-nicht";

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

        // Der *Text* wird bewusst nicht geprueft: bei Reasoning-Modellen haengt er vom
        // Denkverhalten ab, nicht von der Existenz. Verbraucht das Modell trotz allem das
        // ganze Limit im Denken, antwortet die API mit HTTP 400 statt einer abgeschnittenen
        // Antwort — auch das beweist, dass das Modell existiert.
        var (outcome, detail) = await CallAsync(modelId);

        outcome.Should().Be(LiveModelCallOutcome.Exists, detail);
    }

    [SkippableFact]
    public async Task Invented_model_id_is_reported_missing()
    {
        Skip.If(ApiKey is null, "Kein OPENAI_API_KEY gefunden — Live-Pruefung uebersprungen.");

        // Gegenprobe: die Auswertung darf ein totes Modell nicht als vorhanden durchwinken.
        var (outcome, detail) = await CallAsync(InventedModelId);

        outcome.Should().Be(LiveModelCallOutcome.Missing, detail);
    }

    private static async Task<(LiveModelCallOutcome Outcome, string Detail)> CallAsync(string modelId)
    {
        var provider = new OpenAiLlmProvider(ApiKey!);

        try
        {
            await provider.GenerateAsync(
                systemPrompt: "Antworte mit genau einem Wort.",
                userContent: "Sag: ok",
                options: new LlmOptions(MaxTokens: ProbeMaxTokens, Model: modelId));
            return (LiveModelCallOutcome.Exists, "Aufruf ohne Fehler");
        }
        catch (ClientResultException ex)
        {
            return (LiveModelCallClassifier.Classify(ex.Status, ex.Message), ex.Message);
        }
    }
}
