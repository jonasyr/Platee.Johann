namespace Platee.Johann.Application.Interfaces;

/// <summary>Ergebnis einer Modellprüfung.</summary>
public enum ModelProbeResult
{
    /// <summary>Das Modell existiert und ist mit dem hinterlegten Schlüssel erreichbar.</summary>
    Available,

    /// <summary>OpenAI kennt die Id nicht — abgekündigt oder umbenannt.</summary>
    NotFound,

    /// <summary>Ohne Schlüssel lässt sich nichts prüfen.</summary>
    NoApiKey,

    /// <summary>
    /// Die Prüfung war nicht möglich. <b>Nicht</b> dasselbe wie ein fehlendes Modell —
    /// dieser Fall darf das Speichern nie blockieren.
    /// </summary>
    NetworkError,
}

/// <summary>
/// Prüft, ob eine Modell-Id bei OpenAI noch existiert.
/// <para>
/// Die Prüfung kostet keine Token: sie fragt nur die Modell-Beschreibung ab und erzeugt
/// keine Antwort.
/// </para>
/// </summary>
public interface IModelAvailabilityProbe
{
    /// <summary>Prüft eine Modell-Id.</summary>
    /// <param name="modelId">Die zu prüfende API-Bezeichnung.</param>
    /// <param name="ct">Abbruch, etwa weil der Nutzer inzwischen ein anderes Modell wählt.</param>
    /// <returns>Das Prüfergebnis.</returns>
    Task<ModelProbeResult> ProbeAsync(string modelId, CancellationToken ct = default);
}
