using FluentAssertions;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;

namespace Platee.Johann.Tests.Unit;

public class PromptSettingsTests
{
    [Fact]
    public void Default_HasAllPromptValues()
    {
        var ps = PromptSettings.Default;

        ps.SystemMessage.Should().Be(SummaryPrompts.SystemMessage);
        ps.AbstractPrompt.Should().Be(SummaryPrompts.Abstract);
        ps.StructuredPrompt.Should().Be(SummaryPrompts.Structured);
        ps.ProsePrompt.Should().Be(SummaryPrompts.Prose);
        ps.EmailPrompt.Should().Be(SummaryPrompts.Email);
        ps.AufgabePrompt.Should().Be(SummaryPrompts.Aufgabe);
        ps.GespraechsnotizPrompt.Should().Be(SummaryPrompts.Gespraechsnotiz);
        ps.StundenzettelPrompt.Should().Be(SummaryPrompts.Stundenzettel);
        ps.AnalogPrompt.Should().Be(SummaryPrompts.Analog);
        ps.PromptDefaultsRevision.Should().Be(PromptDefaultsMigration.CurrentRevision);
    }

    [Fact]
    public void Default_IsImmutable_WithExpression_ProducesNewInstance()
    {
        var original = PromptSettings.Default;
        var modified = original with { SystemMessage = "custom" };

        modified.SystemMessage.Should().Be("custom");
        original.SystemMessage.Should().Be(SummaryPrompts.SystemMessage);
    }
}
