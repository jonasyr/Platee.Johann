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
    public void Update_sets_both_values_atomically()
    {
        var holder = new SettingsHolder(AppSettings.Default, PromptSettings.Default);

        var newSettings = AppSettings.Default with { Name = "NewName" };
        var newPrompts = PromptSettings.Default with { SystemMessage = "NewSystem" };
        holder.Update(newSettings, newPrompts);

        holder.Current.Should().BeSameAs(newSettings);
        holder.Prompts.Should().BeSameAs(newPrompts);
    }

    [Fact]
    public void Snapshot_after_Update_reflects_both_values()
    {
        var holder = new SettingsHolder(AppSettings.Default, PromptSettings.Default);

        var newSettings = AppSettings.Default with { Name = "Updated" };
        var newPrompts = PromptSettings.Default with { SystemMessage = "Updated" };
        holder.Update(newSettings, newPrompts);

        var snapshot = holder.Snapshot();
        snapshot.Current.Name.Should().Be("Updated");
        snapshot.Prompts.SystemMessage.Should().Be("Updated");
    }

    [Fact]
    public void Current_setter_preserves_existing_Prompts()
    {
        var customPrompts = PromptSettings.Default with { SystemMessage = "Custom" };
        var holder = new SettingsHolder(AppSettings.Default, customPrompts);

        holder.Current = AppSettings.Default with { Name = "NewName" };

        holder.Current.Name.Should().Be("NewName");
        holder.Prompts.SystemMessage.Should().Be("Custom");
    }

    [Fact]
    public void Prompts_setter_preserves_existing_Current()
    {
        var customSettings = AppSettings.Default with { Name = "Custom" };
        var holder = new SettingsHolder(customSettings, PromptSettings.Default);

        holder.Prompts = PromptSettings.Default with { SystemMessage = "NewSystem" };

        holder.Prompts.SystemMessage.Should().Be("NewSystem");
        holder.Current.Name.Should().Be("Custom");
    }

    [Fact]
    public void Snapshot_never_returns_torn_state()
    {
        // Verify that Snapshot always returns a consistent pair:
        // Current and Prompts from the same logical write.
        var holder = new SettingsHolder(
            AppSettings.Default with { Name = "A" },
            PromptSettings.Default with { SystemMessage = "A" });

        var errors = new System.Collections.Concurrent.ConcurrentBag<string>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));

        // Writer thread: alternates between state "A" and state "B"
        var writer = Task.Run(() =>
        {
            var stateA = (
                Settings: AppSettings.Default with { Name = "A" },
                Prompts: PromptSettings.Default with { SystemMessage = "A" });
            var stateB = (
                Settings: AppSettings.Default with { Name = "B" },
                Prompts: PromptSettings.Default with { SystemMessage = "B" });

            var toggle = false;
            while (!cts.Token.IsCancellationRequested)
            {
                var s = toggle ? stateA : stateB;
                holder.Update(s.Settings, s.Prompts);
                toggle = !toggle;
            }
        });

        // Reader thread: takes snapshots and checks consistency
        var reader = Task.Run(() =>
        {
            while (!cts.Token.IsCancellationRequested)
            {
                var snap = holder.Snapshot();
                if (snap.Current.Name != snap.Prompts.SystemMessage)
                {
                    errors.Add($"Torn snapshot: Current.Name={snap.Current.Name}, Prompts.SystemMessage={snap.Prompts.SystemMessage}");
                }
            }
        });

        Task.WhenAll(writer, reader).GetAwaiter().GetResult();
        errors.Should().BeEmpty("Snapshot must always return a consistent Current+Prompts pair");
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
