namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Services;
using Platee.Johann.Application.Settings;
using Platee.Johann.Infrastructure.Json;
using Platee.Johann.UI.ViewModels;

public sealed class SettingsViewModelSaveTargetTests
{
    private static SettingsViewModel CreateSut()
    {
        var repo = Substitute.For<ISettingsRepository>();
        repo.SaveAsync(Arg.Any<AppSettings>()).Returns(Task.CompletedTask);

        var promptRepo = Substitute.For<IPromptSettingsRepository>();
        promptRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
        promptRepo.SaveAsync(Arg.Any<PromptSettings>(), Arg.Any<CancellationToken>()).Returns(Task.CompletedTask);

        var holder = new SettingsHolder(AppSettings.Default, PromptSettings.Default);
        return new SettingsViewModel(repo, promptRepo, holder);
    }

    [Fact]
    public void SaveTarget_DefaultsToPersonal()
    {
        var vm = CreateSut();

        vm.SaveTarget.Should().Be(CategoryScope.Personal);
        vm.IsGlobalTarget.Should().BeFalse();
    }

    [Fact]
    public void IsPromptReadOnly_NoPasswordGateRemains()
    {
        var vm = CreateSut();

        vm.GetType().GetProperty("IsPromptReadOnly").Should().BeNull(
            "the read-only prompt gate is removed together with the admin password");
        vm.GetType().GetProperty("IsAdminMode").Should().BeNull(
            "admin mode is replaced by the explicit save target");
    }

    [Fact]
    public void PromptWarningText_MentionsAllUsers_WhenTargetIsGlobal()
    {
        var vm = CreateSut();

        vm.SaveTarget = CategoryScope.Global;

        vm.IsGlobalTarget.Should().BeTrue();
        vm.PromptWarningText.Should().Contain("alle");
    }

    /// <summary>
    /// #114 (Audit F29): saving with target „Global" wrote every category into the team file,
    /// the user's personal ones included — they then showed up for the whole team as global
    /// templates, and a personal template created in the same session never reached the
    /// personal file.
    /// </summary>
    [Fact]
    public async Task GlobalSave_WritesOnlyGlobalCategoriesToTheTeamFile_AndPersonalOnesToThePersonalFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "johann-114-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var teamFile = Path.Combine(dir, "prompts.json");
        try
        {
            var settings = AppSettings.Default with { GlobalPromptFilePath = teamFile };
            var repo = Substitute.For<ISettingsRepository>();
            repo.SaveAsync(Arg.Any<AppSettings>()).Returns(Task.CompletedTask);

            PromptSettings? savedPersonal = null;
            var promptRepo = Substitute.For<IPromptSettingsRepository>();
            promptRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
            promptRepo
                .SaveAsync(Arg.Do<PromptSettings>(p => savedPersonal = p), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            var vm = new SettingsViewModel(repo, promptRepo, new SettingsHolder(settings, PromptSettings.Default));

            vm.AddCategoryCommand.Execute(null);
            vm.Categories[0].Name = "Meine Privatvorlage";
            vm.AddCategoryCommand.Execute(null);
            vm.Categories[1].Name = "Teamvorlage";
            vm.Categories[1].IsGlobal = true;
            vm.SaveTarget = CategoryScope.Global;

            await vm.SaveCommand.ExecuteAsync(null);

            vm.StatusMessage.Should().Be("✓ Globale Prompts für alle Mitarbeiter gespeichert.");
            var team = File.ReadAllText(teamFile);
            team.Should().Contain("Teamvorlage");
            team.Should().NotContain("Meine Privatvorlage", "persönliche Vorlagen gehören nie in die Team-Datei");

            savedPersonal.Should().NotBeNull("die persönliche Vorlage muss im selben Speichern in die persönliche Datei");
            savedPersonal!.CustomCategories.Should().ContainSingle()
                .Which.Name.Should().Be("Meine Privatvorlage");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// #114 end to end, with real files on both sides: one save with target „Global“ holding a
    /// new personal and a new global template, then both files reloaded and merged exactly as
    /// Johann does at startup (<see cref="PromptSettingsLoader.MergeCategories"/>). The personal
    /// template must come back as personal, the global one as global — and each file must hold
    /// only its own.
    /// </summary>
    [Fact]
    public async Task GlobalSave_RealFiles_AfterRestart_EachTemplateKeepsItsScope()
    {
        var dir = Path.Combine(Path.GetTempPath(), "johann-114-" + Guid.NewGuid().ToString("N"));
        var teamFile = Path.Combine(dir, "team", "prompts.json");
        var homeDir = Path.Combine(dir, "home");
        Directory.CreateDirectory(Path.GetDirectoryName(teamFile)!);
        try
        {
            var settings = AppSettings.Default with { GlobalPromptFilePath = teamFile };
            var repo = Substitute.For<ISettingsRepository>();
            repo.SaveAsync(Arg.Any<AppSettings>()).Returns(Task.CompletedTask);
            var personalRepo = new JsonPromptSettingsRepository(homeDir);

            var vm = new SettingsViewModel(repo, personalRepo, new SettingsHolder(settings, PromptSettings.Default));
            vm.AddCategoryCommand.Execute(null);
            vm.Categories[0].Name = "Privat F29";
            vm.AddCategoryCommand.Execute(null);
            vm.Categories[1].Name = "Team F29";
            vm.Categories[1].IsGlobal = true;
            vm.SaveTarget = CategoryScope.Global;

            await vm.SaveCommand.ExecuteAsync(null);

            // "Restart": read both files fresh, merge as PromptStartupResolver does.
            var team = await JsonPromptSettingsRepository.FromFilePath(teamFile).LoadAsync();
            var personal = await new JsonPromptSettingsRepository(homeDir).LoadAsync();
            var merged = PromptSettingsLoader.MergeCategories(team, personal);

            team.CustomCategories.Select(c => c.Name).Should().Equal("Team F29");
            personal.CustomCategories.Select(c => c.Name).Should().Equal("Privat F29");
            merged.Should().HaveCount(2);
            merged.Single(c => c.Name == "Privat F29").Scope.Should().Be(CategoryScope.Personal);
            merged.Single(c => c.Name == "Team F29").Scope.Should().Be(CategoryScope.Global);
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    /// <summary>
    /// #114: a global category switched to personal must leave the team file and land in the
    /// personal file — in exactly one of the two after a global save.
    /// </summary>
    [Fact]
    public async Task GlobalSave_MovesACategorySwitchedToPersonalOutOfTheTeamFile()
    {
        var dir = Path.Combine(Path.GetTempPath(), "johann-114-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);
        var teamFile = Path.Combine(dir, "prompts.json");
        try
        {
            var existing = new CategoryDefinition
            {
                Id = "custom.baustelle-1a2b",
                Name = "Baustellenbericht",
                Prompt = "Fasse zusammen: {transcript}",
                Scope = CategoryScope.Global,
            };
            var settings = AppSettings.Default with { GlobalPromptFilePath = teamFile };
            var prompts = PromptSettings.Default with { CustomCategories = [existing] };

            var repo = Substitute.For<ISettingsRepository>();
            repo.SaveAsync(Arg.Any<AppSettings>()).Returns(Task.CompletedTask);

            PromptSettings? savedPersonal = null;
            var promptRepo = Substitute.For<IPromptSettingsRepository>();
            promptRepo.LoadAsync(Arg.Any<CancellationToken>()).Returns(PromptSettings.Default);
            promptRepo
                .SaveAsync(Arg.Do<PromptSettings>(p => savedPersonal = p), Arg.Any<CancellationToken>())
                .Returns(Task.CompletedTask);

            var vm = new SettingsViewModel(repo, promptRepo, new SettingsHolder(settings, prompts));
            vm.Categories.Should().ContainSingle().Which.IsGlobal.Should().BeTrue();

            vm.Categories[0].IsGlobal = false;
            vm.SaveTarget = CategoryScope.Global;
            await vm.SaveCommand.ExecuteAsync(null);

            File.ReadAllText(teamFile).Should().NotContain("Baustellenbericht");
            savedPersonal!.CustomCategories.Should().ContainSingle()
                .Which.Id.Should().Be("custom.baustelle-1a2b");
        }
        finally
        {
            Directory.Delete(dir, recursive: true);
        }
    }

    [Fact]
    public void PromptWarningText_DoesNotMentionAllUsers_WhenTargetIsPersonal()
    {
        var vm = CreateSut();

        vm.PromptWarningText.Should().NotContain("alle Nutzer");
    }
}
