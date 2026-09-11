namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Xunit;

/// <summary>
/// #71 Stufe 2 — was ein Diktat ungefaehr kostet.
/// <para>
/// Der Kern des Modells: nur zwei Zahlen je Modell sind gemessen, alles andere wird lokal
/// gerechnet. Dadurch fallen eigene Vorlagen unter dieselbe Formel und brauchen keine
/// Sonderbehandlung — messen liessen sie sich ohnehin nicht, sie entstehen ja erst beim
/// Nutzer.
/// </para>
/// </summary>
public sealed class DictationCostEstimatorTests
{
    private static SummaryModel Luna =>
        SummaryModelCatalog.All.Single(m => m.Id == "gpt-5.6-luna");

    private static AppSettings Recommended =>
        AppSettings.Default with { SectionModes = SectionModeDefaults.Recommended };

    [Fact]
    public void One_minute_with_the_recommended_preset_costs_about_a_third_of_a_cent()
    {
        // Gemessen 2026-09-11 mit exakter Tokenisierung: 0,30 Cent. Diese Schaetzung
        // liefert 0,267 — rund 11 % darunter, weil die durchgehend grossgeschriebene
        // System-Nachricht mit 3,14 Zeichen je Token dichter packt als der Durchschnitt
        // von 4,3. Fuer eine "ungefaehr"-Angabe unerheblich (0,03 Cent), fuer den Test
        // aber eng genug, um ein Abdriften zu bemerken.
        var e = DictationCostEstimator.Estimate(
            Luna,
            PromptSettings.Default,
            Recommended,
            DictationCostEstimator.TokensPerSpeechMinute);

        e.Cents.Should().BeInRange(0.20, 0.40);
    }

    [Fact]
    public void The_recommended_preset_counts_six_sections()
    {
        // Vier Auto-Abschnitte plus die Intrinsics Titel und Kurzfassung, die immer laufen.
        DictationCostEstimator
            .Estimate(Luna, PromptSettings.Default, Recommended, 128)
            .AutoSectionCount.Should().Be(6);
    }

    [Fact]
    public void On_demand_sections_do_not_count()
    {
        var allOnDemand = AppSettings.Default with
        {
            SectionModes = BuiltInSections.All.ToDictionary(id => id, _ => GenerationMode.OnDemand),
        };

        var e = DictationCostEstimator.Estimate(Luna, PromptSettings.Default, allOnDemand, 128);

        e.AutoSectionCount.Should().Be(2);   // nur die Intrinsics
    }

    [Fact]
    public void A_custom_category_set_to_auto_raises_the_estimate()
    {
        var category = new CategoryDefinition
        {
            Id = "custom.test-abcd",
            Name = "Test",
            Prompt = new string('x', 2000),
        };
        var prompts = PromptSettings.Default with { CustomCategories = [category] };
        var withCustom = Recommended with
        {
            SectionModes = new Dictionary<string, GenerationMode>(Recommended.SectionModes)
            {
                [category.Id] = GenerationMode.Auto,
            },
        };

        var without = DictationCostEstimator.Estimate(
            Luna, PromptSettings.Default, Recommended, 128);
        var with = DictationCostEstimator.Estimate(Luna, prompts, withCustom, 128);

        with.AutoSectionCount.Should().Be(without.AutoSectionCount + 1);
        with.Cents.Should().BeGreaterThan(without.Cents);
    }

    [Fact]
    public void A_negative_output_base_never_yields_negative_cost()
    {
        // Sols Grundlast ist negativ gefittet (-22); ein sehr kurzes Diktat duerfte sonst
        // negative Ausgabe-Token und damit negative Kosten ergeben.
        var sol = SummaryModelCatalog.All.Single(m => m.Id == "gpt-5.6-sol");

        DictationCostEstimator.Estimate(sol, PromptSettings.Default, Recommended, 1)
            .Cents.Should().BeGreaterThan(0);
    }

    [Fact]
    public void Longer_dictations_cost_more()
    {
        var shortOne = DictationCostEstimator.Estimate(
            Luna, PromptSettings.Default, Recommended, 128);
        var longOne = DictationCostEstimator.Estimate(
            Luna, PromptSettings.Default, Recommended, 640);

        longOne.Cents.Should().BeGreaterThan(shortOne.Cents);
    }

    [Fact]
    public void Sol_costs_markedly_more_than_luna()
    {
        // Gemessen rund 18x. Die Info-Karte soll genau diesen Abstand sichtbar machen.
        var sol = SummaryModelCatalog.All.Single(m => m.Id == "gpt-5.6-sol");

        var luna = DictationCostEstimator.Estimate(
            Luna, PromptSettings.Default, Recommended,
            DictationCostEstimator.TokensPerSpeechMinute).Cents;
        var solCents = DictationCostEstimator.Estimate(
            sol, PromptSettings.Default, Recommended,
            DictationCostEstimator.TokensPerSpeechMinute).Cents;

        (solCents / luna).Should().BeGreaterThan(10);
    }

    [Theory]
    [InlineData(null, 0)]
    [InlineData("", 0)]
    public void TokensFor_handles_empty_input(string? text, int expected)
    {
        DictationCostEstimator.TokensFor(text).Should().Be(expected);
    }

    [Fact]
    public void TokensFor_uses_the_calibrated_ratio()
    {
        // 4,3 Zeichen je Token, gemessen an 30 deutschen Texten (Median 4,28).
        DictationCostEstimator.TokensFor(new string('x', 430)).Should().Be(100);
    }

    [Fact]
    public void Every_built_in_section_maps_to_a_prompt()
    {
        // Die Zuordnung Abschnitts-Id -> Vorlage ist eine zweite Wahrheit neben
        // EntryProcessingService. Faellt ein Abschnitt dazu und wird hier vergessen,
        // zaehlte er mit Vorlagenlaenge 0 — leise falsch statt laut kaputt.
        var allAuto = AppSettings.Default with
        {
            SectionModes = BuiltInSections.All.ToDictionary(id => id, _ => GenerationMode.Auto),
        };

        var e = DictationCostEstimator.Estimate(Luna, PromptSettings.Default, allAuto, 128);

        e.AutoSectionCount.Should().Be(BuiltInSections.All.Count + 2);
        DictationCostEstimator.MissingTemplates(PromptSettings.Default).Should().BeEmpty();
    }
}
