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
    public void Create_RootInsideInjectedForbiddenRoot_ThrowsAndCreatesNothing()
    {
        var forbiddenParent = Directory.CreateDirectory(Path.Combine(this.tmp, "forbidden")).FullName;
        var root = Path.Combine(forbiddenParent, "sb");

        var act = () => TestSandbox.Create(root, "1.5.0", [forbiddenParent], adjust: null);

        act.Should().Throw<InvalidOperationException>();
        Directory.Exists(root).Should().BeFalse();
    }
}
