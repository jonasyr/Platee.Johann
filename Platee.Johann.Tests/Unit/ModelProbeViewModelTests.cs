namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.UI.ViewModels;
using Xunit;

/// <summary>
/// #71 Stufe 2 — wie die Einstellungsansicht auf das Pruefergebnis reagiert.
/// <para>
/// Die Regel, die am ehesten kaputtgeht: ein Netzwerkfehler darf das Speichern
/// <b>nicht</b> blockieren. Sonst kann im Zug niemand mehr etwas einstellen.
/// </para>
/// </summary>
public sealed class ModelProbeViewModelTests
{
    private static SummaryModel Sol =>
        SummaryModelCatalog.All.Single(m => m.Id == "gpt-5.6-sol");

    private static SummaryModel Terra =>
        SummaryModelCatalog.All.Single(m => m.Id == "gpt-5.6-terra");

    [Fact]
    public async Task A_missing_model_blocks_saving()
    {
        var vm = Sut(ModelProbeResult.NotFound);

        vm.SelectedModel = Sol;
        await vm.WaitForProbeAsync();

        vm.IsSaveBlocked.Should().BeTrue();
        vm.ModelStatusText.Should().Contain("nicht verfügbar");
        vm.SaveCommand.CanExecute(null).Should().BeFalse();
    }

    [Fact]
    public async Task A_network_error_does_not_block_saving()
    {
        var vm = Sut(ModelProbeResult.NetworkError);

        vm.SelectedModel = Sol;
        await vm.WaitForProbeAsync();

        vm.IsSaveBlocked.Should().BeFalse();
        vm.ModelStatusText.Should().Contain("nicht geprüft");
        vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    [Fact]
    public async Task Without_a_key_saving_stays_possible()
    {
        var vm = Sut(ModelProbeResult.NoApiKey);

        vm.SelectedModel = Terra;
        await vm.WaitForProbeAsync();

        vm.IsSaveBlocked.Should().BeFalse();
        vm.ModelStatusText.Should().Contain("Schlüssel");
    }

    [Fact]
    public async Task An_available_model_reports_success()
    {
        var vm = Sut(ModelProbeResult.Available);

        vm.SelectedModel = Terra;
        await vm.WaitForProbeAsync();

        vm.IsSaveBlocked.Should().BeFalse();
        vm.ModelStatusText.Should().Contain("verfügbar");
    }

    [Fact]
    public async Task A_late_answer_for_a_previous_model_is_ignored()
    {
        // Sonst ueberschreibt die Antwort zum abgewaehlten Modell den aktuellen Status —
        // der Nutzer saehe "nicht verfuegbar" fuer ein Modell, das in Ordnung ist.
        var probe = Substitute.For<IModelAvailabilityProbe>();
        probe.ProbeAsync("gpt-5.6-sol", Arg.Any<CancellationToken>())
            .Returns(async call =>
            {
                await Task.Delay(120, call.Arg<CancellationToken>());
                return ModelProbeResult.NotFound;
            });
        probe.ProbeAsync("gpt-5.6-terra", Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(ModelProbeResult.Available));

        var vm = SutWith(probe);
        vm.SelectedModel = Sol;
        vm.SelectedModel = Terra;
        await vm.WaitForProbeAsync();

        vm.IsSaveBlocked.Should().BeFalse();
    }

    [Fact]
    public void Without_a_probe_nothing_is_blocked()
    {
        // Alle uebrigen Tests konstruieren das ViewModel ohne Probe; sie duerfen davon
        // nicht beruehrt werden.
        var vm = SutWith(probe: null);

        vm.IsSaveBlocked.Should().BeFalse();
        vm.SaveCommand.CanExecute(null).Should().BeTrue();
    }

    private static SettingsViewModel Sut(ModelProbeResult result)
    {
        var probe = Substitute.For<IModelAvailabilityProbe>();
        probe.ProbeAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(Task.FromResult(result));
        return SutWith(probe);
    }

    private static SettingsViewModel SutWith(IModelAvailabilityProbe? probe)
    {
        var repo = Substitute.For<ISettingsRepository>();
        repo.SaveAsync(Arg.Any<AppSettings>()).Returns(Task.CompletedTask);

        var promptRepo = Substitute.For<IPromptSettingsRepository>();
        promptRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        promptRepo.SaveAsync(Arg.Any<PromptSettings>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var holder = new SettingsHolder(AppSettings.Default, PromptSettings.Default);

        return new SettingsViewModel(repo, promptRepo, holder, probe: probe);
    }
}
