namespace Platee.Johann.Application.Processing;

/// <summary>Ein für Zusammenfassungen wählbares Modell.</summary>
/// <param name="Id">Die API-Bezeichnung, die an OpenAI geht.</param>
/// <param name="DisplayName">Der Name, den der Nutzer sieht — nie die rohe Id.</param>
public sealed record SummaryModel(string Id, string DisplayName);

/// <summary>
/// Die Modelle, unter denen der Nutzer wählen darf.
/// <para>
/// Fest im Code und nicht aus der API geladen: <c>GET /v1/models</c> liefert nur Ids,
/// keine Anzeigenamen und keine Einstufungen. Die Prüfung gegen die API kommt in Stufe 2;
/// die Darstellung kommt immer von hier.
/// </para>
/// <para>
/// Stand 2026-09-10, Quelle: https://developers.openai.com/api/docs/pricing — der Katalog
/// veraltet absehbar. Das Sicherheitsnetz ist <see cref="SummaryModelResolver"/>: eine Id in
/// den Einstellungen, die hier fehlt, führt zum Standard und einem Hinweis, nicht zu
/// scheiternden Diktaten.
/// </para>
/// </summary>
public static class SummaryModelCatalog
{
    /// <summary>Gets die Modelle in der Reihenfolge, in der sie angeboten werden.</summary>
    public static IReadOnlyList<SummaryModel> All { get; } =
    [
        new(ModelNames.Summaries, "GPT-5.6 Luna"),
        new("gpt-5.6-terra", "GPT-5.6 Terra"),
        new("gpt-5.6-sol", "GPT-5.6 Sol"),
        new("gpt-5-nano", "GPT-5 Nano"),
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
