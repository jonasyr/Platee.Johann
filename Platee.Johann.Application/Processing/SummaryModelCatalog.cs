namespace Platee.Johann.Application.Processing;

/// <summary>Ein für Zusammenfassungen wählbares Modell.</summary>
/// <param name="Id">Die API-Bezeichnung, die an OpenAI geht.</param>
/// <param name="DisplayName">Der Name, den der Nutzer sieht — nie die rohe Id.</param>
/// <param name="OneLiner">Ein Satz, was das Modell gut kann. Ohne Technikkram.</param>
/// <param name="Reasoning">Denkleistung als 1–4 gefüllte Punkte.</param>
/// <param name="Speed">Tempo als 1–4 Blitze.</param>
/// <param name="PriceInPerMillion">USD je 1 Mio. Eingabe-Token.</param>
/// <param name="PriceOutPerMillion">USD je 1 Mio. Ausgabe-Token.</param>
/// <param name="OutputBase">
/// Grundlast der Ausgabe in Token. Kann negativ gefittet sein — beim Rechnen auf 0 abschneiden.
/// </param>
/// <param name="OutputSlope">Wie viele Ausgabe-Token je Transkript-Token entstehen.</param>
public sealed record SummaryModel(
    string Id,
    string DisplayName,
    string OneLiner,
    int Reasoning,
    int Speed,
    double PriceInPerMillion,
    double PriceOutPerMillion,
    double OutputBase,
    double OutputSlope);

/// <summary>
/// Die Modelle, unter denen der Nutzer wählen darf.
/// <para>
/// Fest im Code und nicht aus der API geladen: <c>GET /v1/models</c> liefert nur Ids,
/// keine Anzeigenamen und keine Einstufungen. Die Prüfung gegen die API kommt in Stufe 2;
/// die Darstellung kommt immer von hier.
/// </para>
/// <para>
/// Stand 2026-09-11, Quelle: https://developers.openai.com/api/docs/pricing — der Katalog
/// veraltet absehbar. Das Sicherheitsnetz ist <see cref="SummaryModelResolver"/>: eine Id in
/// den Einstellungen, die hier fehlt, führt zum Standard und einem Hinweis, nicht zu
/// scheiternden Diktaten.
/// </para>
/// <para>
/// <c>gpt-5-nano</c> stand hier bis zum 2026-09-11 als „billigste Option". Die Messung hat
/// das widerlegt: es verbrennt 2.496 Denk-Token für 514 sichtbare und ist dadurch
/// <b>je Diktat teurer als Luna</b>, bei schwächerer Qualität. Geprüfte Ersatzkandidaten
/// (<c>gpt-5-mini</c>, <c>gpt-5.4-mini</c>) sind ebenfalls von Luna dominiert — deshalb
/// drei Modelle statt vier.
/// </para>
/// <para>
/// <see cref="SummaryModel.OutputBase"/> und <see cref="SummaryModel.OutputSlope"/> stammen
/// aus einer Messung am 2026-09-11: 16 erfundene Diktate von 20 s bis 6 min, 160 Aufrufe,
/// kleinste Quadrate über Transkriptlänge → Ausgabe-Token. Sie schließen die Denk-Token ein,
/// weil OpenAI die als Ausgabe abrechnet, und gelten für das Standardverhalten <b>ohne</b>
/// gesetzten <c>reasoning_effort</c>. Setzt #73 später einen, müssen sie neu gemessen werden.
/// </para>
/// </summary>
public static class SummaryModelCatalog
{
    /// <summary>Gets die Modelle in der Reihenfolge, in der sie angeboten werden.</summary>
    public static IReadOnlyList<SummaryModel> All { get; } =
    [
        new(
            ModelNames.Summaries,
            "GPT-5.6 Luna",
            "Schreibt zügig gute Zusammenfassungen — für den Alltag die richtige Wahl.",
            Reasoning: 3,
            Speed: 4,
            PriceInPerMillion: 0.20,
            PriceOutPerMillion: 1.20,
            OutputBase: 62,
            OutputSlope: 1.05),
        new(
            "gpt-5.6-terra",
            "GPT-5.6 Terra",
            "Denkt gründlicher und trifft bei verschachtelten Diktaten öfter den Kern.",
            Reasoning: 4,
            Speed: 3,
            PriceInPerMillion: 2.00,
            PriceOutPerMillion: 12.00,
            OutputBase: 4,
            OutputSlope: 1.22),
        new(
            "gpt-5.6-sol",
            "GPT-5.6 Sol",
            "Die gründlichste Stufe — für lange, schwierige Aufnahmen, wenn es darauf ankommt.",
            Reasoning: 4,
            Speed: 2,
            PriceInPerMillion: 4.00,
            PriceOutPerMillion: 20.00,
            OutputBase: -22,
            OutputSlope: 1.53),
    ];

    /// <summary>Gets das Modell, das gilt, solange der Nutzer nichts anderes wählt.</summary>
    public static SummaryModel Default { get; } = All[0];

    /// <summary>Liefert das Modell zur Id, oder <c>null</c> wenn der Katalog sie nicht kennt.</summary>
    /// <param name="id">Die gesuchte API-Bezeichnung.</param>
    /// <returns>Das passende Modell oder <c>null</c>.</returns>
    public static SummaryModel? TryFind(string? id) =>
        string.IsNullOrWhiteSpace(id)
            ? null
            : All.FirstOrDefault(m => string.Equals(m.Id, id, StringComparison.Ordinal));
}
