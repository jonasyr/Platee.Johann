using FluentAssertions;
using Platee.Johann.Application.Settings;
using Platee.Johann.UI;

namespace Platee.Johann.Tests.Unit;

public sealed class StartupPathResolverTests
{
    [Fact]
    public void Resolve_WhenConfiguredPathsAreUsable_KeepsPersistedAndEffectivePathsAligned()
    {
        var settings = AppSettings.Default with
        {
            Quellverzeichnis = "input",
            Archivverzeichnis = "archive",
            Ausgabeverzeichnis = "output",
        };

        var result = StartupPathResolver.Resolve(
            settings,
            [],
            ValidateKnownPaths("input", "archive", "output"),
            () => "default-input",
            () => "default-output");

        result.PersistedSettings.Should().Be(settings);
        result.EffectiveSettings.Quellverzeichnis.Should().Be("input");
        result.EffectiveSettings.Archivverzeichnis.Should().Be("archive");
        result.EffectiveSettings.Ausgabeverzeichnis.Should().Be("output");
        result.Issues.Should().BeEmpty();
    }

    [Fact]
    public void Resolve_WhenConfiguredPathsAreInvalid_PreservesSettingsAndUsesRuntimeFallbacks()
    {
        var settings = AppSettings.Default with
        {
            Quellverzeichnis = "bad-input",
            Archivverzeichnis = "bad-archive",
            Ausgabeverzeichnis = "bad-output",
        };

        var result = StartupPathResolver.Resolve(
            settings,
            [],
            path => path.StartsWith("bad", StringComparison.Ordinal)
                ? new DirectoryValidationResult(false, $"cannot use {path}")
                : new DirectoryValidationResult(true),
            () => "default-input",
            () => "default-output");

        result.PersistedSettings.Quellverzeichnis.Should().Be("bad-input");
        result.PersistedSettings.Archivverzeichnis.Should().Be("bad-archive");
        result.PersistedSettings.Ausgabeverzeichnis.Should().Be("bad-output");

        result.EffectiveSettings.Quellverzeichnis.Should().Be("default-input");
        result.EffectiveSettings.Archivverzeichnis.Should().Be(Path.Combine("default-input", "Archiv"));
        result.EffectiveSettings.Ausgabeverzeichnis.Should().Be("default-output");

        result.Issues.Should().HaveCount(3);
        result.Issues.Select(i => i.Label).Should().Contain(["Quellverzeichnis", "Archivverzeichnis", "Ausgabeverzeichnis"]);
    }

    [Fact]
    public void Resolve_WhenOutputPathIsInvalid_UsesValidCliOutputAsRuntimeFallback()
    {
        var settings = AppSettings.Default with
        {
            Quellverzeichnis = "input",
            Archivverzeichnis = "archive",
            Ausgabeverzeichnis = "bad-output",
        };

        var result = StartupPathResolver.Resolve(
            settings,
            ["cli-output"],
            ValidateKnownPaths("input", "archive", "cli-output"),
            () => "default-input",
            () => "default-output");

        result.EffectiveSettings.Ausgabeverzeichnis.Should().Be("cli-output");
        result.Issues.Should().ContainSingle();
        result.Issues[0].FallbackPath.Should().Be("cli-output");
    }

    [Fact]
    public void Resolve_WhenConfiguredPathIsBlank_AddsWarningAndUsesFallback()
    {
        var settings = AppSettings.Default with
        {
            Quellverzeichnis = "",
            Archivverzeichnis = "",
            Ausgabeverzeichnis = "",
        };

        var result = StartupPathResolver.Resolve(
            settings,
            [],
            _ => new DirectoryValidationResult(true),
            () => "default-input",
            () => "default-output");

        result.EffectiveSettings.Quellverzeichnis.Should().Be("default-input");
        result.EffectiveSettings.Archivverzeichnis.Should().Be(Path.Combine("default-input", "Archiv"));
        result.EffectiveSettings.Ausgabeverzeichnis.Should().Be("default-output");
        result.Issues.Should().HaveCount(3);
        result.Issues.All(i => i.Reason == "Pfad ist leer.").Should().BeTrue();
    }

    private static Func<string, DirectoryValidationResult> ValidateKnownPaths(params string[] usablePaths)
        => path => usablePaths.Contains(path, StringComparer.Ordinal)
            ? new DirectoryValidationResult(true)
            : new DirectoryValidationResult(false, $"cannot use {path}");
}
