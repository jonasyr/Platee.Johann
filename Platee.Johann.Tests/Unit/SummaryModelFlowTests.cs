using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// #71 — das gewaehlte Modell muss beim Provider ankommen und je Lauf eingefroren sein.
/// <para>
/// Das Einfrieren ist kein Selbstzweck: die Einstellungsansicht ist nicht modal, ein
/// Speichern mitten in acht parallelen Aufrufen wuerde sonst das Modell im laufenden
/// Vorgang wechseln.
/// </para>
/// </summary>
public sealed class SummaryModelFlowTests
{
    [Fact]
    public async Task The_configured_model_reaches_the_provider()
    {
        var holder = new SettingsHolder(
            AppSettings.Default with { SummaryModel = "gpt-5.6-sol" },
            PromptSettings.Default);
        var llm = FakeLlm();

        await new SummaryGenerator(llm, holder).GenerateAbstractAsync("test transcript");

        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<LlmOptions>(o => o.Model == "gpt-5.6-sol"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task A_model_change_mid_run_does_not_affect_the_running_one()
    {
        var holder = new SettingsHolder(
            AppSettings.Default with { SummaryModel = "gpt-5.6-luna" },
            PromptSettings.Default);
        var llm = FakeLlm();

        var scoped = new SummaryGenerator(llm, holder).WithSnapshot();

        // Der Nutzer speichert mitten im Lauf ein anderes Modell.
        holder.Current = holder.Current with { SummaryModel = "gpt-5.6-sol" };

        await scoped.GenerateAbstractAsync("test transcript");

        await llm.Received(1).GenerateAsync(
            Arg.Any<string>(),
            Arg.Any<string>(),
            Arg.Is<LlmOptions>(o => o.Model == "gpt-5.6-luna"),
            Arg.Any<CancellationToken>());
    }

    private static ILlmProvider FakeLlm()
    {
        var llm = Substitute.For<ILlmProvider>();
        llm.IsAvailable.Returns(true);
        llm.GenerateAsync(
                Arg.Any<string>(),
                Arg.Any<string>(),
                Arg.Any<LlmOptions>(),
                Arg.Any<CancellationToken>())
            .Returns("summary");
        return llm;
    }
}
