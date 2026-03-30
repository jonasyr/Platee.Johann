namespace Platee.Johann.Application.Processing;

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
        "Das Transkript kann sehr lang sein - analysiere alle Inhalte gründlich.\n\n" +
        "Erstelle eine strukturierte Zusammenfassung. Verwende nur die Abschnitte, die relevant sind. Ordne jeden Inhalt nur der passendsten der folgenden Überschriften zu\n\n" +
        "### Kontext\n" +
        "(Worum geht es, was ist der Anlass, prägnant, kurz)\n\n" +
        "### Kernaussagen\n" +
        "(Die wichtigsten Inhalte, Themen, Argumente, darf ausführlich sein, keine Entscheidungen oder To-Dos)\n\n" +
        "### Entscheidungen\n" +
        "(Was wurde beschlossen oder festgestellt, keine todos)\n\n" +
        "### Offene Punkte / ToDos\n" +
        "(nenne konkrete Aktionen die erfolgen müssen und wenn möglich ergänzen wer Handlung durchführen soll, kurz und prägnant)\n\n" +
        "Regeln:\n" +
        "- Bevorzuge Informationsdichte statt extreme Kürze\n" +
        "- prägnante Zusammenfassung, aber inhaltlich vollständig\n" +
        "- Jede Information darf nur einer Überschrift zugeordnet werden, entscheide zu welcher Überschrift eine Information am besten passt\n" +
        "- verwende nur die im Transkript genannten Informationen, keine Annahmen oder Interpretationen hinzufügen\n" +
        "- Nutze Unterpunkte und Aufzählungen für Struktur\n" +
        "- Lass irrelevante Abschnitte weg, berücksichtige Inhalte als relevant, wenn sie mindestens eines der folgenden Kriterien erfüllen: enthalten eine Entscheidung, ein Ergebnis oder eine Schlussfolgerung, führen zu einer konkreten Handlung oder einem ToDo, betreffen das Hauptthema oder Ziel des Gesprächs, werden mehrfach erwähnt oder besonders betont\n" +
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
        "- Betreff: Kurz, Prägnant, aussagekräftig (beginne mit \"Betreff: \")\n" +
        "- Ton: Professionell, persönlich, freundlich, kollegial\n" +
        "- Inhalt: Die wichtigsten Punkte klar und präzise kommunizieren\n" +
        "- Struktur: gut gegliedert, leicht lesbar, verständlich\n" +
        "- Länge: So kompakt wie möglich bei vollständiger Information\n" +
        "- mit Grußformel beginnen, wenn möglich: Namen des Empfängers erkennen und in Begrüßungsformel integrieren (erkennbar beispielsweise an \"Lieber/Liebe...\" zu Beginn des Transkripts), falls nicht erkennbar: neutrale Formulierung\n" +
        "- Abschluss: Dank und Einladung, Rückfragen zu stellen, keine Grußformel am Ende der Mail, falls vorhanden: Handlungsaufforderung\n" +
        "- keine Informationen ergänzen, die nicht aus Transkript hervorgehen\n" +
        "- Ich-Perspektive ausgehend vom Sprecher\n" +
        "- Vermeide Wiederholungen von Inhalten oder Formulierungen; fasse ähnliche Punkte zusammen\n" +
        "- siezen\n\n" +
        "Stil:\n" +
        "- Höflich und respektvoll\n" +
        "- Direkt und klar (keine unnötigen Floskeln)\n" +
        "- Aktive Sprache, kurze und vollständige Sätze\n" +
        "- Fließtext, keine Stichpunkte\n" +
        "- Professionelles Deutsch\n" +
        "- positive Sprache\n\n" +
        "Zusammenfassung:\n{prose_summary}";

    public const string Aufgabe =
        "Du erhältst ein Transkript eines Sprach-Diktats auf Deutsch.\n\n" +
        "Gebe in einem Satz den Kontext an.\n\n" +
        "Extrahiere die im Transkript genannten Aufgaben.\n" +
        "Struktur:\n" +
        "- Je Aufgabe ein Stichpunkt\n" +
        "- fasse zusammengehörige Handlungen zu einer Aufgabe zusammen\n" +
        "- falls vorhanden: nenne Frist\n" +
        "- falls vorhanden: nenne Person die Aufgabe ausführen soll\n\n" +
        "Regeln:\n" +
        "- nutze nur Informationen die explizit im Transkript stehen\n" +
        "- keine Ergänzungen oder Annahmen\n" +
        "- kurz und präzise formulieren\n" +
        "- keine Dopplungen\n" +
        "- chronologische Abfolge beibehalten\n" +
        "- Korrigiere dabei offensichtliche Transkriptions- und Spracherkennungsfehler.\n\n" +
        "Transkript:\n{transcript}";

    public const string Gespraechsnotiz =
        "Du erhältst ein Transkript eines Gesprächs auf Deutsch.\n" +
        "Erstelle eine strukturierte, kundentaugliche Gesprächsnotiz (Teilnehmer, Themen, Beschlüsse, weiteres Vorgehen).\n\n" +
        "Transkript:\n{transcript}";

    public const string Stundenzettel =
        "Du erhältst ein Transkript eines Sprach-Diktats auf Deutsch.\n" +
        "Extrahiere die Zeiten und Tätigkeiten, um sie in einen Stundenzettel einzutragen.\n\n" +
        "Transkript:\n{transcript}";

    public const string Analog =
        "Du erhältst ein Transkript eines analogen Eintrags auf Deutsch.\n" +
        "Fasse den Eintrag treffend zusammen.\n\n" +
        "Transkript:\n{transcript}";
}
