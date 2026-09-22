namespace Platee.Johann.Tests.Unit;

/// <summary>Was ein fehlgeschlagener Live-Aufruf ueber das Modell aussagt.</summary>
internal enum LiveModelCallOutcome
{
    /// <summary>Das Modell hat geantwortet — auch wenn die Antwort nicht fertig wurde.</summary>
    Exists,

    /// <summary>OpenAI kennt die Id nicht (abgekuendigt oder umbenannt).</summary>
    Missing,

    /// <summary>Schluessel, Kontingent, Server — sagt nichts ueber das Modell.</summary>
    Unexpected,
}

/// <summary>
/// Wertet den Fehler eines Live-Aufrufs aus (#84). Reasoning-Modelle, die ihr Token-Limit
/// vollstaendig im Denken verbrauchen, beantwortet die API mit HTTP 400 statt mit einer
/// abgeschnittenen Antwort — das beweist, dass das Modell existiert.
/// </summary>
internal static class LiveModelCallClassifier
{
    private const string TokenLimitText = "max_tokens or model output limit";
    private const string ModelNotFoundCode = "model_not_found";

    public static LiveModelCallOutcome Classify(int status, string message)
    {
        if (status == 404 || message.Contains(ModelNotFoundCode, StringComparison.Ordinal))
        {
            return LiveModelCallOutcome.Missing;
        }

        return status == 400 && message.Contains(TokenLimitText, StringComparison.Ordinal)
            ? LiveModelCallOutcome.Exists
            : LiveModelCallOutcome.Unexpected;
    }
}
