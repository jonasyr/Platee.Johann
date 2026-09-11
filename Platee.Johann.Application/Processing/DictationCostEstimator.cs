namespace Platee.Johann.Application.Processing;

using Platee.Johann.Application.Settings;

/// <summary>Was ein Diktat mit der aktuellen Einstellung ungefähr kostet.</summary>
/// <param name="Cents">Geschätzte Kosten in Cent (USD-Preise, 1:1 gerechnet).</param>
/// <param name="AutoSectionCount">Wie viele Abschnitte dabei erzeugt werden.</param>
/// <param name="TranscriptTokens">Die zugrunde gelegte Transkriptlänge.</param>
public sealed record CostEstimate(double Cents, int AutoSectionCount, int TranscriptTokens);

/// <summary>
/// Schätzt die Kosten eines Diktats aus der tatsächlichen Konfiguration des Nutzers.
/// <para>
/// Nur zwei Zahlen je Modell sind gemessen (<see cref="SummaryModel.OutputBase"/> und
/// <see cref="SummaryModel.OutputSlope"/>); die Eingabeseite rechnet diese Klasse lokal.
/// Dadurch fallen <b>eigene Vorlagen unter dieselbe Formel</b> — ihr Prompttext steht in den
/// Einstellungen und wird gezählt wie der einer eingebauten. Eine Sonderbehandlung wäre auch
/// gar nicht möglich: fremde Vorlagen entstehen erst beim Nutzer und lassen sich nicht vorab
/// vermessen.
/// </para>
/// <para>
/// Die Zahl trägt in der Oberfläche bewusst das Wort „ungefähr": die Ausgabelänge schwankt,
/// und die Denk-Token eines Modells können sich mit einem Update ändern.
/// </para>
/// </summary>
public static class DictationCostEstimator
{
    /// <summary>
    /// Zeichen je Token. Median 4,28 über 30 deutsche Texte — erfundene Diktate, echte
    /// Prompt-Vorlagen und echte Transkripte, gemessen am 2026-09-11 mit
    /// <c>tiktoken/o200k_base</c>. Für eine „ungefähr"-Angabe genau genug; ein Tokenizer
    /// als Abhängigkeit wäre dafür unverhältnismäßig.
    /// <para>
    /// ⚠ Die Schätzung liegt dadurch systematisch <b>rund 10 % unter</b> der exakten
    /// Tokenisierung: die durchgehend großgeschriebene System-Nachricht packt mit 3,14
    /// Zeichen je Token deutlich dichter als der Durchschnitt und ist zugleich der größte
    /// Eingabeposten. Bewusst nicht durch eine passend gebogene Konstante kaschiert — bei
    /// 0,3 Cent je Diktat geht es um 0,03 Cent, und ein geschönter Wert wäre schwerer zu
    /// durchschauen als ein dokumentierter. Schreibt #73 die System-Nachricht in
    /// gemischter Schreibweise neu, verschwindet die Abweichung von selbst.
    /// </para>
    /// </summary>
    private const double CharsPerToken = 4.3;

    /// <summary>
    /// Der Titel-Prompt steht fest im <see cref="SummaryGenerator"/> und nicht in den
    /// Einstellungen. Hier nur zum Mitzählen gespiegelt — es zählt die Länge, nicht der
    /// Wortlaut.
    /// </summary>
    private const string TitlePromptText =
        "Bitte formuliere einen sehr kurzen, prägnanten Titel (maximal 3-7 Worte) für den "
        + "folgenden Text. Antworte NUR mit dem Titel, ohne Anführungszeichen oder Erklärungen:";

    /// <summary>
    /// Gets Transkript-Token je Minute Sprechzeit. Aus dem echten Archiv abgeleitet
    /// (439 Wörter / 676 Token in 317 s).
    /// </summary>
    public static int TokensPerSpeechMinute => 128;

    /// <summary>Grobe Tokenzahl eines Textes.</summary>
    /// <param name="text">Der zu zählende Text.</param>
    /// <returns>Geschätzte Tokenzahl, 0 bei leerem Text.</returns>
    public static int TokensFor(string? text) =>
        string.IsNullOrEmpty(text) ? 0 : (int)Math.Round(text.Length / CharsPerToken);

    /// <summary>
    /// Schätzt die Kosten eines Diktats.
    /// </summary>
    /// <param name="model">Das gewählte Modell mit seinen gemessenen Konstanten.</param>
    /// <param name="prompts">Die Prompt-Einstellungen, inklusive eigener Vorlagen.</param>
    /// <param name="settings">Die Nutzereinstellungen, insbesondere die Abschnitts-Modi.</param>
    /// <param name="transcriptTokens">Angenommene Transkriptlänge in Token.</param>
    /// <returns>Kosten in Cent, Anzahl erzeugter Abschnitte und die Bezugsgröße.</returns>
    public static CostEstimate Estimate(
        SummaryModel model,
        PromptSettings prompts,
        AppSettings settings,
        int transcriptTokens)
    {
        var systemTokens = TokensFor(prompts.SystemMessage);
        var templateTokens = CollectTemplateTokens(prompts, settings);

        // Negativ gefittete Grundlast (Sol) darf nie zu negativer Ausgabe führen.
        var outputPerCall = Math.Max(0, model.OutputBase + (model.OutputSlope * transcriptTokens));

        var usd = 0.0;
        foreach (var template in templateTokens)
        {
            var input = systemTokens + template + transcriptTokens;
            usd += input * model.PriceInPerMillion / 1_000_000;
            usd += outputPerCall * model.PriceOutPerMillion / 1_000_000;
        }

        return new CostEstimate(usd * 100, templateTokens.Count, transcriptTokens);
    }

    /// <summary>
    /// Liefert die eingebauten Abschnitts-Ids, denen <see cref="TemplateFor"/> keine Vorlage
    /// zuordnet.
    /// <para>
    /// Die Zuordnung ist eine zweite Wahrheit neben <c>EntryProcessingService</c>. Käme ein
    /// eingebauter Abschnitt dazu und würde hier vergessen, ginge er mit Vorlagenlänge 0 in
    /// die Rechnung — leise falsch. Ein Test darüber macht es laut.
    /// </para>
    /// </summary>
    /// <param name="prompts">Die Prompt-Einstellungen.</param>
    /// <returns>Die nicht zugeordneten Abschnitts-Ids.</returns>
    public static IReadOnlyList<string> MissingTemplates(PromptSettings prompts) =>
        [.. BuiltInSections.All.Where(id => string.IsNullOrWhiteSpace(TemplateFor(id, prompts)))];

    private static List<int> CollectTemplateTokens(PromptSettings prompts, AppSettings settings)
    {
        // Titel und Kurzfassung sind Intrinsics: sie laufen immer, unabhängig von den Modi.
        var result = new List<int>
        {
            TokensFor(TitlePromptText),
            TokensFor(prompts.AbstractPrompt),
        };

        foreach (var section in SectionCatalog.Build(prompts, settings.SectionModes))
        {
            if (section.Mode != GenerationMode.Auto)
            {
                continue;
            }

            result.Add(TokensFor(
                section.Category is { } category
                    ? category.Prompt
                    : TemplateFor(section.Id, prompts)));
        }

        return result;
    }

    private static string TemplateFor(string sectionId, PromptSettings prompts) => sectionId switch
    {
        BuiltInSections.LongSummary => prompts.StructuredPrompt,
        BuiltInSections.ProseSummary => prompts.ProsePrompt,
        BuiltInSections.TaskList => prompts.AufgabePrompt,
        BuiltInSections.ConversationNote => prompts.GespraechsnotizPrompt,
        BuiltInSections.EmailText => prompts.EmailPrompt,
        BuiltInSections.Stundenzettel => prompts.StundenzettelPrompt,
        BuiltInSections.Analog => prompts.AnalogPrompt,
        _ => string.Empty,
    };
}
