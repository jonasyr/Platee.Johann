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
        var act = () => AuditSandbox.Create(root, this.tmp, null);
        act.Should().Throw<InvalidOperationException>();
    }
}
