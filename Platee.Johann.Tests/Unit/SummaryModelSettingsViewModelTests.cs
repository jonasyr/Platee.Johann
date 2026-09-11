namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Processing;
using Platee.Johann.Application.Settings;
using Platee.Johann.UI.ViewModels;

/// <summary>
/// #71 — der Abschnitt „KI-Modell“ in den Einstellungen.
/// <para>
/// Der heikle Fall ist eine gespeicherte Id, die der Katalog nicht kennt: die Auswahlliste
/// darf dann nicht leer bleiben, sonst kaeme der Nutzer nicht mehr an ein gueltiges Modell.
/// </para>
/// </summary>
public sealed class SummaryModelSettingsViewModelTests
{
    [Fact]
    public void The_selected_model_mirrors_the_stored_setting()
    {
        var (vm, _) = CreateSut(AppSettings.Default with { SummaryModel = "gpt-5.6-sol" });

        vm.SelectedModel.Id.Should().Be("gpt-5.6-sol");
    }

    [Fact]
    public void An_unknown_stored_model_selects_the_default_instead_of_nothing()
    {
        var (vm, _) = CreateSut(AppSettings.Default with { SummaryModel = "gpt-4o" });

        vm.SelectedModel.Id.Should().Be(SummaryModelCatalog.Default.Id);
    }

    [Fact]
    public void All_catalog_models_are_offered()
    {
        var (vm, _) = CreateSut(AppSettings.Default);

        vm.AvailableModels.Should().HaveCount(3);
    }

    [Fact]
    public async Task Saving_persists_the_selected_model()
    {
        var (vm, repo) = CreateSut(AppSettings.Default);

        vm.SelectedModel = SummaryModelCatalog.All.Single(m => m.Id == "gpt-5.6-terra");
        await vm.SaveCommand.ExecuteAsync(null);

        await repo.Received(1).SaveAsync(
            Arg.Is<AppSettings>(s => s.SummaryModel == "gpt-5.6-terra"));
    }

    private static (SettingsViewModel Vm, ISettingsRepository Repo) CreateSut(AppSettings settings)
    {
        var repo = Substitute.For<ISettingsRepository>();
        repo.SaveAsync(Arg.Any<AppSettings>()).Returns(Task.CompletedTask);

        var promptRepo = Substitute.For<IPromptSettingsRepository>();
        promptRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        promptRepo.SaveAsync(Arg.Any<PromptSettings>(), Arg.Any<CancellationToken>())
            .Returns(Task.CompletedTask);

        var holder = new SettingsHolder(settings, PromptSettings.Default);

        return (new SettingsViewModel(repo, promptRepo, holder), repo);
    }
}
