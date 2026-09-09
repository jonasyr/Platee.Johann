namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using NSubstitute;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Application.Services;
using Platee.Johann.Application.Settings;

/// <summary>
/// Covers the personal half of prompt startup.
/// <para>
/// Storage rule: the team file on the share owns the eight built-in prompts and any global
/// categories; the local personal file owns only the user's own categories. A personal file
/// therefore can never override a team prompt, so a user cannot be silently frozen out of
/// baseline updates — the divergence shape that #45 H1 was about.
/// </para>
/// </summary>
public sealed class PersonalCategoryStartupTests
{
    [Fact]
    public async Task ResolveAsync_MergesPersonalCategoriesOnTopOfTeamPrompts()
    {
        var team = PromptSettings.Default with
        {
            SystemMessage = "TEAM-SYSTEM",
            CustomCategories = [Cat("custom.team", "Team-Kategorie")],
        };
        var personal = PromptSettings.Default with
        {
            CustomCategories = [Cat("custom.eigen", "Eigene Kategorie")],
        };

        var result = await PromptStartupResolver.ResolveAsync(
            Repo(PromptSettings.Default),
            Repo(team),
            @"Z:\prompts.json",
            personalRepo: Repo(personal));

        result.Prompts.SystemMessage.Should().Be("TEAM-SYSTEM");
        result.Prompts.CustomCategories.Select(c => c.Id)
            .Should().BeEquivalentTo(["custom.team", "custom.eigen"]);
    }

    [Fact]
    public async Task ResolveAsync_TagsScopeByOriginatingFile()
    {
        var team = PromptSettings.Default with { CustomCategories = [Cat("custom.team", "T")] };
        var personal = PromptSettings.Default with { CustomCategories = [Cat("custom.eigen", "E")] };

        var result = await PromptStartupResolver.ResolveAsync(
            Repo(PromptSettings.Default), Repo(team), @"Z:\prompts.json", personalRepo: Repo(personal));

        result.Prompts.CustomCategories.Single(c => c.Id == "custom.team").Scope
            .Should().Be(CategoryScope.Global);
        result.Prompts.CustomCategories.Single(c => c.Id == "custom.eigen").Scope
            .Should().Be(CategoryScope.Personal);
    }

    [Fact]
    public async Task ResolveAsync_PersonalCategoryWinsOnIdCollision()
    {
        var team = PromptSettings.Default with { CustomCategories = [Cat("custom.x", "Team-Name")] };
        var personal = PromptSettings.Default with { CustomCategories = [Cat("custom.x", "Mein-Name")] };

        var result = await PromptStartupResolver.ResolveAsync(
            Repo(PromptSettings.Default), Repo(team), @"Z:\prompts.json", personalRepo: Repo(personal));

        result.Prompts.CustomCategories.Should().HaveCount(1);
        result.Prompts.CustomCategories[0].Name.Should().Be("Mein-Name");
    }

    [Fact]
    public async Task ResolveAsync_PersonalPromptsNeverOverrideTeamPrompts()
    {
        var team = PromptSettings.Default with { SystemMessage = "TEAM" };
        var personal = PromptSettings.Default with { SystemMessage = "PERSÖNLICH-SOLL-IGNORIERT-WERDEN" };

        var result = await PromptStartupResolver.ResolveAsync(
            Repo(PromptSettings.Default), Repo(team), @"Z:\prompts.json", personalRepo: Repo(personal));

        result.Prompts.SystemMessage.Should().Be(
            "TEAM", "the personal file owns only categories, never the team's prompt text");
    }

    [Fact]
    public async Task ResolveAsync_WithoutAPersonalRepo_BehavesExactlyAsBefore()
    {
        var team = PromptSettings.Default with { SystemMessage = "TEAM" };

        var result = await PromptStartupResolver.ResolveAsync(
            Repo(PromptSettings.Default), Repo(team), @"Z:\prompts.json");

        result.Prompts.SystemMessage.Should().Be("TEAM");
        result.Prompts.CustomCategories.Should().BeEmpty();
        result.Warning.Should().BeNull();
    }

    [Fact]
    public async Task ResolveAsync_PersonalCategoriesSurviveAnUnreachableShare()
    {
        // The whole point of the personal file: an offline VPN must not cost the user
        // their own categories.
        var cached = PromptSettings.Default with { SystemMessage = "ZWISCHENSPEICHER" };
        var personal = PromptSettings.Default with { CustomCategories = [Cat("custom.eigen", "E")] };

        var globalRepo = Substitute.For<IPromptSettingsRepository>();
        globalRepo.IsReachable.Returns(false);

        var result = await PromptStartupResolver.ResolveAsync(
            Repo(cached), globalRepo, @"Z:\prompts.json", personalRepo: Repo(personal));

        result.Prompts.CustomCategories.Should().ContainSingle(c => c.Id == "custom.eigen");
        result.Warning.Should().NotBeNull();
    }

    private static CategoryDefinition Cat(string id, string name) =>
        new() { Id = id, Name = name, Prompt = "P {transcript}" };

    private static IPromptSettingsRepository Repo(PromptSettings settings)
    {
        var repo = Substitute.For<IPromptSettingsRepository>();
        repo.IsReachable.Returns(true);
        repo.LastLoadReadFile.Returns(true);
        repo.LastLoadFault.Returns((SettingsFileFault?)null);
        repo.LoadAsync(Arg.Any<CancellationToken>()).Returns(settings);
        return repo;
    }
}
