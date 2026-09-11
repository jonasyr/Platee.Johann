using FluentAssertions;
using Platee.Johann.Application.Processing;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// #71 — der Nutzer waehlt das Modell fuer die Zusammenfassungen selbst.
/// <para>
/// Der Katalog steht fest im Code: die API liefert nur Ids, keine Anzeigenamen und keine
/// Einstufungen. Diese Tests pinnen, was dem Nutzer angeboten wird.
/// </para>
/// </summary>
public sealed class SummaryModelCatalogTests
{
    [Fact]
    public void Catalog_lists_the_three_offered_models()
    {
        SummaryModelCatalog.All.Select(m => m.Id).Should().Equal(
            "gpt-5.6-luna", "gpt-5.6-terra", "gpt-5.6-sol");
    }

    [Fact]
    public void Nano_is_gone_because_it_costs_more_per_dictation_than_luna()
    {
        // Gemessen 2026-09-11: 2.496 Denk-Token fuer 514 sichtbare, dadurch 1,5x Luna
        // bei schwaecherer Qualitaet. In beiden Dimensionen unterlegen — es anzubieten
        // hiesse, eine Falle zu bauen: wer es wegen "guenstig" waehlt, zahlt mehr.
        SummaryModelCatalog.TryFind("gpt-5-nano").Should().BeNull();
    }

    [Fact]
    public void Every_model_carries_the_measured_cost_constants()
    {
        foreach (var m in SummaryModelCatalog.All)
        {
            m.PriceInPerMillion.Should().BeGreaterThan(0);
            m.PriceOutPerMillion.Should().BeGreaterThan(m.PriceInPerMillion);
            m.OutputSlope.Should().BeGreaterThan(0);
            m.OneLiner.Should().NotBeNullOrWhiteSpace();
            m.Reasoning.Should().BeInRange(1, 4);
            m.Speed.Should().BeInRange(1, 4);
        }
    }

    [Fact]
    public void Default_is_luna()
    {
        SummaryModelCatalog.Default.Id.Should().Be("gpt-5.6-luna");
        SummaryModelCatalog.Default.DisplayName.Should().Be("GPT-5.6 Luna");
    }

    [Fact]
    public void Default_matches_the_ModelNames_constant()
    {
        // Sonst zeigten Katalog und Konstante auf verschiedene Modelle.
        SummaryModelCatalog.Default.Id.Should().Be(ModelNames.Summaries);
    }

    [Fact]
    public void TryFind_returns_the_model_for_a_known_id()
    {
        SummaryModelCatalog.TryFind("gpt-5.6-sol")!.DisplayName.Should().Be("GPT-5.6 Sol");
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("gpt-5-nano")]
    [InlineData("")]
    [InlineData(null)]
    public void TryFind_returns_null_for_anything_else(string? id)
    {
        SummaryModelCatalog.TryFind(id).Should().BeNull();
    }
}
