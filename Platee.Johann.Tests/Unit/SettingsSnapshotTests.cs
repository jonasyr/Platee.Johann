using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Settings;

namespace Platee.Johann.Tests.Unit;

public sealed class SettingsSnapshotTests
{
    [Fact]
    public void Snapshot_captures_current_references()
    {
        var original = new SettingsHolder(AppSettings.Default, PromptSettings.Default);
        var snapshot = original.Snapshot();

        snapshot.Current.Should().BeSameAs(original.Current);
        snapshot.Prompts.Should().BeSameAs(original.Prompts);
    }

    [Fact]
    public void Snapshot_is_isolated_from_later_changes()
    {
        var original = new SettingsHolder(AppSettings.Default, PromptSettings.Default);
        var snapshot = original.Snapshot();

        var newSettings = AppSettings.Default with { Name = "Changed" };
        var newPrompts = PromptSettings.Default with { SystemMessage = "Changed" };
        original.Current = newSettings;
        original.Prompts = newPrompts;

        snapshot.Current.Should().NotBeSameAs(newSettings);
        snapshot.Current.Name.Should().NotBe("Changed");
        snapshot.Prompts.Should().NotBeSameAs(newPrompts);
        snapshot.Prompts.SystemMessage.Should().NotBe("Changed");
    }

    [Fact]
    public async Task SummaryGenerator_WithSnapshot_uses_frozen_settings()
    {
        var holder = new SettingsHolder(
            AppSettings.Default,
            PromptSettings.Default with { AbstractPrompt = "Original prompt: {word_limit} {transcript}" });

        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(
                Arg.Any<string>(), Arg.Any<string>(),
                Arg.Any<LlmOptions>(),
                Arg.Any<CancellationToken>())
            .Returns("summary");

        var generator = new Platee.Johann.Application.Processing.SummaryGenerator(llm, holder);
        var scoped = generator.WithSnapshot();

        // Change settings AFTER creating the scoped generator
        holder.Prompts = PromptSettings.Default with { AbstractPrompt = "Changed prompt: {word_limit} {transcript}" };

        await scoped.GenerateAbstractAsync("test transcript");

        // The scoped generator should have used "Original prompt", not "Changed prompt"
        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Is<string>(s => s.Contains("Original prompt")),
            Arg.Any<LlmOptions>(),
            Arg.Any<CancellationToken>());
    }
}
