namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Settings;

public sealed class CategoryDefinitionTests
{
    [Fact]
    public void CategoryDefinition_DefaultsToPersonalScopeAnd20000Tokens()
    {
        var c = new CategoryDefinition
        {
            Id = "custom.programmierung",
            Name = "Programmierung",
            Prompt = "Fasse zusammen: {transcript}",
        };

        c.Scope.Should().Be(CategoryScope.Personal);
        c.Order.Should().Be(0);
        c.MaxTokens.Should().Be(20000);
    }

    [Fact]
    public void BuiltInSections_All_ContainsSevenDisplayableSections()
    {
        BuiltInSections.All.Should().HaveCount(7);
        BuiltInSections.All.Should().Contain(BuiltInSections.LongSummary);
        BuiltInSections.All.Should().NotContain(
            "builtin.abstract",
            "Abstract is an intrinsic that drives the entry list, not a toggleable section");
    }

    [Fact]
    public void BuiltInSections_DisplayNameOf_ReturnsGermanName()
    {
        BuiltInSections.DisplayNameOf(BuiltInSections.TaskList).Should().Be("Aufgaben");
    }

    [Fact]
    public void BuiltInSections_DisplayNameOf_UnknownId_FallsBackToTheIdItself()
    {
        BuiltInSections.DisplayNameOf("custom.unbekannt").Should().Be("custom.unbekannt");
    }

    [Theory]
    [InlineData("Zusammenfassung", "builtin.longSummary")]
    [InlineData("Ausführliche Zusammenfassung", "builtin.proseSummary")]
    [InlineData("Aufgaben", "builtin.taskList")]
    [InlineData("Gesprächsnotiz", "builtin.conversationNote")]
    [InlineData("E-Mail", "builtin.emailText")]
    [InlineData("Stundenzettel", "builtin.stundenzettel")]
    [InlineData("Analog", "builtin.analog")]
    public void FromLegacyName_PreservesTheOriginalInvertedMapping(string legacy, string expectedId)
    {
        // "Zusammenfassung" maps to LongSummary and "Ausführliche Zusammenfassung" to
        // ProseSummary. That inversion exists in the original ReprocessSectionAsync
        // switch; the mapping must reproduce it exactly, not silently "fix" it.
        BuiltInSections.FromLegacyName(legacy).Should().Be(expectedId);
    }

    [Fact]
    public void FromLegacyName_UnknownName_ReturnsNull()
    {
        BuiltInSections.FromLegacyName("Gibt es nicht").Should().BeNull();
    }
}
