using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// #71 — ein Modell, das es nicht mehr gibt, darf nicht jedes Diktat scheitern lassen.
/// <para>
/// Nach dem Vorbild von <c>StartupPathResolver</c>: die gespeicherte Wahl bleibt unangetastet
/// und wird nur zur Laufzeit ueberstimmt.
/// </para>
/// </summary>
public sealed class SummaryModelResolverTests
{
    [Fact]
    public void A_known_model_passes_through_without_an_issue()
    {
        var result = SummaryModelResolver.Resolve(
            AppSettings.Default with { SummaryModel = "gpt-5-nano" });

        result.EffectiveModelId.Should().Be("gpt-5-nano");
        result.Issue.Should().BeNull();
    }

    [Fact]
    public void An_unknown_model_falls_back_to_the_default_and_reports_an_issue()
    {
        var result = SummaryModelResolver.Resolve(
            AppSettings.Default with { SummaryModel = "gpt-4o" });

        result.EffectiveModelId.Should().Be(SummaryModelCatalog.Default.Id);
        result.Issue.Should().NotBeNull();
        result.Issue.Should().Contain("gpt-4o");
        result.Issue.Should().Contain(SummaryModelCatalog.Default.DisplayName);
    }

    [Fact]
    public void An_empty_model_falls_back_silently()
    {
        // Leer heisst "nie gewaehlt", nicht "kaputt" — das ist kein Anlass fuer einen Hinweis.
        var result = SummaryModelResolver.Resolve(
            AppSettings.Default with { SummaryModel = string.Empty });

        result.EffectiveModelId.Should().Be(SummaryModelCatalog.Default.Id);
        result.Issue.Should().BeNull();
    }

    [Fact]
    public void Resolve_does_not_mutate_the_persisted_choice()
    {
        var persisted = AppSettings.Default with { SummaryModel = "gpt-4o" };

        SummaryModelResolver.Resolve(persisted);

        // Die gespeicherte Wahl wird nur ueberstimmt, nie ueberschrieben.
        persisted.SummaryModel.Should().Be("gpt-4o");
    }
}
