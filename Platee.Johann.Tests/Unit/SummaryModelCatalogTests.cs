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
    public void Catalog_lists_the_four_offered_models()
    {
        SummaryModelCatalog.All.Select(m => m.Id).Should().Equal(
            "gpt-5.6-luna", "gpt-5.6-terra", "gpt-5.6-sol", "gpt-5-nano");
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
        SummaryModelCatalog.TryFind("gpt-5-nano")!.DisplayName.Should().Be("GPT-5 Nano");
    }

    [Theory]
    [InlineData("gpt-4o")]
    [InlineData("")]
    [InlineData(null)]
    public void TryFind_returns_null_for_anything_else(string? id)
    {
        SummaryModelCatalog.TryFind(id).Should().BeNull();
    }
}
