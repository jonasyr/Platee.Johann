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

    [Fact]
    public void SettingsHolder_SeparatesSettingsAndPrompts()
    {
        var settings = AppSettings.Default with { Name = "Test" };
        var prompts = PromptSettings.Default with { SystemMessage = "custom" };
        var holder = new SettingsHolder(settings, prompts);

        holder.Current.Name.Should().Be("Test");
        holder.Prompts.SystemMessage.Should().Be("custom");

        // AppSettings should not contain prompt properties
        typeof(AppSettings).GetProperty("SystemMessage").Should().BeNull(
            because: "AppSettings no longer contains prompt properties");
        typeof(AppSettings).GetProperty("AbstractPrompt").Should().BeNull(
            because: "AppSettings no longer contains prompt properties");
    }

    [Fact]
    public void SettingsHolder_DefaultPrompts_WhenNotProvided()
    {
        var holder = new SettingsHolder(AppSettings.Default);

        holder.Prompts.SystemMessage.Should().Be(SummaryPrompts.SystemMessage);
        holder.Prompts.AbstractPrompt.Should().Be(SummaryPrompts.Abstract);
        holder.Prompts.StructuredPrompt.Should().Be(SummaryPrompts.Structured);
        holder.Prompts.ProsePrompt.Should().Be(SummaryPrompts.Prose);
        holder.Prompts.EmailPrompt.Should().Be(SummaryPrompts.Email);
    }
}
