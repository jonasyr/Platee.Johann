namespace Platee.Johann.UI.Helpers;

/// <summary>
/// Breite einer Spalte, die ihren breitesten Eintrag ungekürzt zeigt — wie ein Doppelklick auf
/// die Spaltengrenze in Excel (#96). Das Ausmessen der Zeilen macht das Fenster; hier steht
/// nur die Rechnung, damit sie ohne Oberfläche testbar ist.
/// </summary>
public static class ColumnAutoFit
{
    /// <param name="widestContent">Gewünschte Breite der breitesten Zeile, ungekürzt gemessen.</param>
    /// <param name="chrome">Was um den Inhalt herum liegt: Zeilenrand, Innenabstand, Bildlaufleiste.</param>
    /// <param name="min">Mindestbreite der Spalte.</param>
    /// <param name="max">Höchstbreite, bei der die Nachbarspalte noch ihre Mindestbreite behält.</param>
    /// <returns>Die neue Breite, oder <c>null</c> ohne Inhalt — dann bleibt die Spalte, wie sie ist.</returns>
    public static double? Width(double widestContent, double chrome, double min, double max)
    {
        if (widestContent <= 0)
        {
            return null;
        }

        // Aufrunden: schon ein Bruchteil eines Pixels zu wenig kürzt den Titel wieder mit „…“.
        var fitted = Math.Ceiling(widestContent + chrome);
        return Math.Max(min, Math.Min(fitted, max));
    }
}
