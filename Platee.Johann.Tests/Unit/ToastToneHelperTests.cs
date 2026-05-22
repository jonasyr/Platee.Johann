namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UI.ViewModels;

public sealed class ToastToneHelperTests
{
    [Theory]
    [InlineData("Fehler: Verbindung abgebrochen", ToastTone.Error)]
    [InlineData("FEHLER: etwas schiefgelaufen", ToastTone.Error)]
    [InlineData("Kein API-Schlüssel konfiguriert.", ToastTone.Warn)]
    [InlineData("Kein API-Schlüssel vorhanden", ToastTone.Warn)]
    [InlineData("Fertig", ToastTone.Ok)]
    [InlineData("5 Einträge geladen", ToastTone.Ok)]
    [InlineData("✓ Als erledigt markiert.", ToastTone.Ok)]
    public void DeriveFromCompletion_ReturnsTone(string message, ToastTone expected)
    {
        ToastToneHelper.DeriveFromCompletion(message).Should().Be(expected);
    }

    [Fact]
    public void DeriveFromCompletion_EmptyMessage_ReturnsOk()
    {
        ToastToneHelper.DeriveFromCompletion(string.Empty).Should().Be(ToastTone.Ok);
    }
}
