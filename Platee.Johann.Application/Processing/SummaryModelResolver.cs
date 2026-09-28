namespace Platee.Johann.Application.Processing;

using Platee.Johann.Application.Settings;

/// <summary>Das zur Laufzeit gültige Modell und ein optionaler Hinweis für den Nutzer.</summary>
/// <param name="EffectiveModelId">Die Id, mit der tatsächlich generiert wird.</param>
/// <param name="Issue">Klartext für den Startdialog, oder <c>null</c> wenn alles stimmt.</param>
public sealed record SummaryModelResolution(string EffectiveModelId, string? Issue);

/// <summary>
/// Prüft die gespeicherte Modellwahl gegen den Katalog.
/// <para>
/// Nach dem Vorbild von <c>StartupPathResolver</c>: die gespeicherte Einstellung bleibt
/// unangetastet, sie wird nur zur Laufzeit überstimmt. Sonst verlöre der Nutzer seine Wahl
/// stillschweigend, sobald er einmal mit einer älteren Version startet.
/// </para>
/// <para>
/// Stufe 1 prüft nur die Katalogzugehörigkeit, nicht die Existenz bei OpenAI — es gibt hier
/// keinen Netzaufruf. Die echte Existenzprüfung kommt in Stufe 2.
/// </para>
/// </summary>
public static class SummaryModelResolver
{
    /// <summary>Ermittelt das gültige Modell zu den gespeicherten Einstellungen.</summary>
    /// <param name="persisted">Die Einstellungen, wie sie auf der Platte stehen.</param>
    /// <returns>Effektive Modell-Id plus optionalem Hinweis.</returns>
    public static SummaryModelResolution Resolve(AppSettings persisted)
    {
        var configured = persisted.SummaryModel;

        if (string.IsNullOrWhiteSpace(configured))
        {
            return new SummaryModelResolution(SummaryModelCatalog.Default.Id, null);
        }

        if (SummaryModelCatalog.TryFind(configured) is not null)
        {
            return new SummaryModelResolution(configured, null);
        }

        return new SummaryModelResolution(
            SummaryModelCatalog.Default.Id,
            $"Das eingestellte Modell „{configured}“ ist nicht mehr verfuegbar. "
            + $"Johann verwendet stattdessen „{SummaryModelCatalog.Default.DisplayName}“. "
            + "Bitte waehlen Sie in den Einstellungen unter „KI-Modell“ ein Modell aus.");
    }
}
