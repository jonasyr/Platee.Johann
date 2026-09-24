namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UiDriver.Sandbox;
using Xunit;

public sealed class AuditSandboxTests : IDisposable
{
    private readonly string tmp = Directory.CreateTempSubdirectory("audit-").FullName;

    public void Dispose() => Directory.Delete(this.tmp, recursive: true);

    [Fact]
    public void Create_CopiesAndRedirects_AndPassesGuard()
    {
        var realHome = Directory.CreateDirectory(Path.Combine(this.tmp, "real")).FullName;
        File.WriteAllText(
            Path.Combine(realHome, "settings.json"),
            $$"""{"name":"JW","ausgabeverzeichnis":{{System.Text.Json.JsonSerializer.Serialize(Path.Combine(realHome, "output"))}},"globalPromptFilePath":"Z:\\12_Tools\\Peano\\Johann\\prompts.json"}""");
        File.WriteAllText(Path.Combine(realHome, "prompts.personal.json"), "{}");
        File.WriteAllText(Path.Combine(realHome, ".env"), "OPENAI_API_KEY=sk-x");
        Directory.CreateDirectory(Path.Combine(realHome, "output", "2026-09-23", "_raw"));
        File.WriteAllText(Path.Combine(realHome, "prompts.cache.somehash.json"), "{}");
        var team = Path.Combine(this.tmp, "team.json");
        File.WriteAllText(team, "{}");

        var layout = AuditSandbox.Create(Path.Combine(this.tmp, "sb"), realHome, team);

        File.Exists(Path.Combine(layout.Home, ".env")).Should().BeTrue();
        File.Exists(layout.TeamPrompts).Should().BeTrue();
        Directory.Exists(Path.Combine(layout.Output, "2026-09-23", "_raw")).Should().BeTrue();
        SandboxGuard.Violations(File.ReadAllText(layout.SettingsFile), [@"Z:\", realHome]).Should().BeEmpty();
        File.ReadAllText(layout.SettingsFile).Should().Contain("\"name\": \"JW\""); // Rest bleibt erhalten
        Directory.GetFiles(layout.Home, "prompts.cache.*.json").Should().BeEmpty();
    }

    [Fact]
    public void Create_RefusesExistingNonEmptyRoot()
    {
        var root = Directory.CreateDirectory(Path.Combine(this.tmp, "sb")).FullName;
        File.WriteAllText(Path.Combine(root, "x"), "x");
        var realHome = Directory.CreateDirectory(Path.Combine(this.tmp, "real")).FullName;

        var act = () => AuditSandbox.Create(root, realHome, null);

        act.Should().Throw<InvalidOperationException>().WithMessage("*existiert bereits*");
    }

    [Fact]
    public void Create_RootInsideInjectedForbiddenRoot_ThrowsAndCreatesNothing()
    {
        var forbiddenParent = Directory.CreateDirectory(Path.Combine(this.tmp, "forbidden")).FullName;
        var root = Path.Combine(forbiddenParent, "sb");
        var realHome = Directory.CreateDirectory(Path.Combine(this.tmp, "real")).FullName;

        var act = () => AuditSandbox.Create(root, realHome, null, [forbiddenParent]);

        act.Should().Throw<InvalidOperationException>();
        Directory.Exists(root).Should().BeFalse();
    }

    [Fact]
    public void Create_RootInsideRealOutput_ThrowsAndCreatesNothing()
    {
        var realHome = Directory.CreateDirectory(Path.Combine(this.tmp, "real")).FullName;
        var realOutput = Directory.CreateDirectory(Path.Combine(realHome, "output")).FullName;
        var root = Path.Combine(realOutput, "sb");

        var act = () => AuditSandbox.Create(root, realHome, null);

        act.Should().Throw<InvalidOperationException>();
        Directory.Exists(root).Should().BeFalse();
    }

    [Fact]
    public void Create_CopiesOutputFolderNamedInRealSettings_NotJustDefault()
    {
        var realHome = Directory.CreateDirectory(Path.Combine(this.tmp, "real")).FullName;
        var customOutput = Directory.CreateDirectory(Path.Combine(this.tmp, "custom-output")).FullName;
        Directory.CreateDirectory(Path.Combine(customOutput, "2026-09-24", "_raw"));
        File.WriteAllText(
            Path.Combine(realHome, "settings.json"),
            $$"""{"name":"JW","ausgabeverzeichnis":{{System.Text.Json.JsonSerializer.Serialize(customOutput)}}}""");

        var layout = AuditSandbox.Create(Path.Combine(this.tmp, "sb"), realHome, null);

        Directory.Exists(Path.Combine(layout.Output, "2026-09-24", "_raw")).Should().BeTrue();
    }

    [Fact]
    public void Create_RootInsideConfiguredOutputOutsideRealHome_ThrowsAndCreatesNothing()
    {
        var realHome = Directory.CreateDirectory(Path.Combine(this.tmp, "real")).FullName;
        var externalOutput = Directory.CreateDirectory(Path.Combine(this.tmp, "external-output")).FullName;
        File.WriteAllText(
            Path.Combine(realHome, "settings.json"),
            $$"""{"name":"JW","ausgabeverzeichnis":{{System.Text.Json.JsonSerializer.Serialize(externalOutput)}}}""");
        var root = Path.Combine(externalOutput, "sb");

        var act = () => AuditSandbox.Create(root, realHome, null);

        act.Should().Throw<InvalidOperationException>();
        Directory.Exists(root).Should().BeFalse();
    }

    [Fact]
    public void Create_NonStringAusgabeverzeichnisInRealSettings_FallsBackWithoutThrowing()
    {
        var realHome = Directory.CreateDirectory(Path.Combine(this.tmp, "real")).FullName;
        File.WriteAllText(Path.Combine(realHome, "settings.json"), """{"name":"JW","ausgabeverzeichnis":123}""");

        var act = () => AuditSandbox.Create(Path.Combine(this.tmp, "sb"), realHome, null);

        act.Should().NotThrow();
    }

    [Fact]
    public void Create_RelativeAusgabeverzeichnisInRealSettings_FallsBackToRealHomeOutput()
    {
        var realHome = Directory.CreateDirectory(Path.Combine(this.tmp, "real")).FullName;
        Directory.CreateDirectory(Path.Combine(realHome, "output", "2026-09-24", "_raw"));
        File.WriteAllText(
            Path.Combine(realHome, "settings.json"),
            """{"name":"JW","ausgabeverzeichnis":"relative\\output"}""");

        var layout = AuditSandbox.Create(Path.Combine(this.tmp, "sb"), realHome, null);

        Directory.Exists(Path.Combine(layout.Output, "2026-09-24", "_raw")).Should().BeTrue();
    }
}
