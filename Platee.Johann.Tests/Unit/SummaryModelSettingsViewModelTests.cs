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

    [Fact]
    public void The_card_names_a_price_and_the_section_count()
    {
        var (vm, _) = CreateSut(AppSettings.Default with
        {
            SectionModes = SectionModeDefaults.Recommended,
        });

        vm.ModelCostText.Should().Contain("Cent").And.Contain("ungefähr").And.Contain("10 Diktate");
        vm.ModelComparisonText.Should().Contain("6");   // sechs automatische Abschnitte
    }

    [Fact]
    public void The_cheapest_model_is_labelled_as_such_instead_of_compared()
    {
        var (vm, _) = CreateSut(AppSettings.Default);

        vm.SelectedModel = SummaryModelCatalog.Default;

        vm.ModelComparisonText.Should().Contain("günstigste");
    }

    [Fact]
    public void A_dearer_model_is_compared_against_the_cheapest()
    {
        var (vm, _) = CreateSut(AppSettings.Default);

        vm.SelectedModel = SummaryModelCatalog.All.Single(m => m.Id == "gpt-5.6-sol");

        vm.ModelComparisonText.Should().Contain("teurer").And.Contain("GPT-5.6 Luna");
    }

    [Fact]
    public void The_dots_and_bolts_match_the_catalog()
    {
        var (vm, _) = CreateSut(AppSettings.Default);

        vm.SelectedModel = SummaryModelCatalog.Default;

        vm.ModelReasoningDots.Should().Be("●●●○");
        vm.ModelSpeedBolts.Should().Be("⚡⚡⚡⚡");
    }

    [Fact]
    public void Switching_a_template_to_automatic_changes_the_cost_text()
    {
        // Der Zusammenhang soll sichtbar werden: mehr automatische Abschnitte, mehr Kosten.
        var (vm, _) = CreateSut(AppSettings.Default with
        {
            SectionModes = SectionModeDefaults.Recommended,
        });
        var before = vm.ModelCostText;

        var offRow = vm.BuiltInSectionModes.First(r => !r.IsAuto);
        offRow.IsAuto = true;

        vm.ModelCostText.Should().NotBe(before);
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
