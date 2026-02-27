namespace Johann.Application.Processing;

/// <summary>
/// German-language prompts for GPT-based summarization.
/// Mirrors the Python config.py prompts exactly.
/// Placeholders: {word_limit}, {transcript}, {prose_summary}.
/// </summary>
public static class SummaryPrompts
{
    public const string SystemMessage =
        "Du bist ein Experte für professionelle deutsche Geschäftskommunikation. " +
        "Deine Aufgabe: Sprach-Diktate in klare, strukturierte Zusammenfassungen umwandeln. " +
        "Stil: Präzise, sachlich, gut lesbar.";

    public const string Abstract =
        "Du erhältst ein Transkript eines Sprach-Diktats auf Deutsch.\n" +
        "Erstelle ein kurzes Abstract, das die wichtigsten Punkte zusammenfasst.\n\n" +
        "Das Abstract soll:\n" +
        "- Maximal {word_limit} Wörter umfassen\n" +
        "- Die Kernaussagen und Hauptthemen klar hervorheben\n" +
        "- Prägnant formuliert sein, keine Ausschmückungen\n" +
        "- Nur relevante Informationen aus dem Transkript enthalten\n" +
        "- Als eigenständige Kurzübersicht funktionieren\n\n" +
        "Transkript:\n{transcript}";

    public const string Structured =
        "Du erhältst ein Transkript eines Sprach-Diktats auf Deutsch.\n" +
        "Das Transkript kann sehr lang sein - nimm dir Zeit, alle Inhalte gründlich zu analysieren.\n\n" +
        "Erstelle eine strukturierte Zusammenfassung. Verwende NUR die Abschnitte, die relevant sind:\n\n" +
        "### Kontext & Ziel\n" +
        "(Worum geht es, was ist der Anlass)\n\n" +
        "### Hauptpunkte\n" +
        "(Die wichtigsten Inhalte/Argumente - bei langen Transkripten sei ausführlich)\n\n" +
        "### Entscheidungen / Erkenntnisse\n" +
        "(Was wurde beschlossen oder festgestellt)\n\n" +
        "### Offene Punkte / ToDos\n" +
        "(Was muss noch geklärt oder erledigt werden)\n\n" +
        "### Zusätzliche Details\n" +
        "(Nur bei sehr langen Transkripten: wichtige Nebenpunkte, die nicht oben passen)\n\n" +
        "Regeln:\n" +
        "- Maximal {word_limit} Wörter insgesamt\n" +
        "- Fasse prägnant zusammen, fokussiere auf Kernpunkte\n" +
        "- Keine unnötigen Details oder Ausschmückungen\n" +
        "- Nutze Unterpunkte und Aufzählungen für Klarheit\n" +
        "- Lass irrelevante Abschnitte weg\n" +
        "- Verwende ### für Hauptüberschriften\n\n" +
        "Transkript:\n{transcript}";

    public const string Prose =
        "Du erhältst ein Transkript eines Sprach-Diktats auf Deutsch.\n" +
        "Erstelle eine professionelle Fließtext-Zusammenfassung.\n\n" +
        "Die Zusammenfassung soll:\n" +
        "- Die wesentlichen Inhalte des Transkripts abdecken\n" +
        "- In perfektem Deutsch mit klarer, präziser Grammatik geschrieben sein\n" +
        "- Als natürlich fließender, gut lesbarer Text formuliert sein (keine Stichpunkte oder Listen)\n" +
        "- Die wichtigsten Details, Überlegungen und Entscheidungen enthalten\n" +
        "- Logisch strukturiert sein mit Absätzen für thematische Übergänge\n" +
        "- Professionell und sachlich klingen, aber verständlich bleiben\n\n" +
        "Stil:\n" +
        "- Prägnant und strukturiert schreiben\n" +
        "- Beschränke dich auf die wesentlichen Punkte aus dem Transkript\n" +
        "- Keine unnötigen Ausschmückungen oder generischen Phrasen\n" +
        "- Neutrale, objektive Perspektive\n" +
        "- Fachbegriffe beibehalten und korrekt verwenden\n" +
        "- Klare, vollständige Sätze\n" +
        "- Zusammenhängender Textfluss mit natürlichen Übergängen\n\n" +
        "Transkript:\n{transcript}";

    public const string Email =
        "Du erhältst eine Zusammenfassung eines Sprach-Diktats.\n" +
        "Erstelle daraus eine professionelle, freundliche E-Mail.\n\n" +
        "Anforderungen:\n" +
        "- Betreff: Prägnant und aussagekräftig (beginne mit \"Betreff: \")\n" +
        "- Ton: Professionell aber persönlich, nicht steif oder übermäßig formell\n" +
        "- Inhalt: Die wichtigsten Punkte klar und präzise kommunizieren\n" +
        "- Struktur: Gut gegliedert, leicht lesbar\n" +
        "- Länge: So kompakt wie möglich bei vollständiger Information\n" +
        "- Abschluss: Freundliche, passende Grußformel\n\n" +
        "Stil:\n" +
        "- Höflich und respektvoll\n" +
        "- Direkt und klar (keine unnötigen Floskeln)\n" +
        "- Aktive Sprache, vollständige Sätze\n" +
        "- Professionelles Deutsch\n\n" +
        "Zusammenfassung:\n{prose_summary}";
}
