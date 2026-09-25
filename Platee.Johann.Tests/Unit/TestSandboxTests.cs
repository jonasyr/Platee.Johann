namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UiDriver.Sandbox;
using Xunit;

public sealed class TestSandboxTests : IDisposable
{
    private readonly string tmp = Directory.CreateTempSubdirectory("testsandbox-").FullName;

    public void Dispose() => Directory.Delete(this.tmp, recursive: true);

    [Fact]
    public void Create_WithForbiddenGlobalPromptFilePath_Throws()
    {
        var root = Path.Combine(this.tmp, "sb");

        var act = () => TestSandbox.Create(
            root,
            "1.5.0",
            adjust: s =>
            {
                s["globalPromptFilePath"] = @"Z:\x";
                return s;
            });

        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void Create_WritesExplicitNullTeamPath()
    {
        var layout = TestSandbox.Create(Path.Combine(this.tmp, "sb"), "1.5.0");

        using var doc = System.Text.Json.JsonDocument.Parse(File.ReadAllText(layout.SettingsFile));
        doc.RootElement.GetProperty("globalPromptFilePath").ValueKind
            .Should().Be(System.Text.Json.JsonValueKind.Null, "null is what the app stores for no team file and marks the key as persisted; an empty string looks like a changed path on the first save");
    }

    [Fact]
    public void Create_RootInsideInjectedForbiddenRoot_ThrowsAndCreatesNothing()
    {
        var forbiddenParent = Directory.CreateDirectory(Path.Combine(this.tmp, "forbidden")).FullName;
        var root = Path.Combine(forbiddenParent, "sb");

        var act = () => TestSandbox.Create(root, "1.5.0", [forbiddenParent], adjust: null);

        act.Should().Throw<InvalidOperationException>();
        Directory.Exists(root).Should().BeFalse();
    }

    [Fact]
    public void Create_RefusesExistingNonEmptyRoot()
    {
        var root = Directory.CreateDirectory(Path.Combine(this.tmp, "sb")).FullName;
        var existingFile = Path.Combine(root, "x");
        File.WriteAllText(existingFile, "x");

        var act = () => TestSandbox.Create(root, "1.5.0");

        act.Should().Throw<InvalidOperationException>().WithMessage("*existiert bereits*");
        File.ReadAllText(existingFile).Should().Be("x");
    }
}
