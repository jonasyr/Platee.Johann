using FluentAssertions;
using Platee.Johann.Application.Processing;
using Xunit;

namespace Platee.Johann.Tests.Unit;

/// <summary>
/// #71 — die Statusleiste nennt das tatsaechlich verwendete Modell.
/// <para>
/// Vor #71 bestand die Zeile aus zwei Konstanten. Waere sie so geblieben, haette sie nach
/// der Umstellung dauerhaft Luna angezeigt, egal was eingestellt ist.
/// </para>
/// </summary>
public sealed class StatusBarModelLabelTests
{
    [Fact]
    public void The_label_names_the_chosen_model_by_its_display_name()
    {
        ModelNames.StatusBarLabelFor("gpt-5.6-terra")
            .Should().Be("gpt-transcribe · GPT-5.6 Terra");
    }

    [Fact]
    public void An_unknown_id_is_shown_raw_rather_than_hidden()
    {
        // Ehrlicher als still den Standard anzuzeigen: der Nutzer sieht, dass etwas klemmt.
        ModelNames.StatusBarLabelFor("gpt-4o")
            .Should().Be("gpt-transcribe · gpt-4o");
    }

    [Fact]
    public void A_missing_id_falls_back_to_the_default_display_name()
    {
        ModelNames.StatusBarLabelFor(null)
            .Should().Be("gpt-transcribe · GPT-5.6 Luna");
    }
}
