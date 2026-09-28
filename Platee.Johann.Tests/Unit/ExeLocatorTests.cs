namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.UiTests;

/// <summary>
/// <see cref="ExeLocator.FindSolutionRoot"/> is the pure, filesystem-only part of locating the
/// Johann exe for the (unrun) UI tests — walking up from an arbitrary start directory until
/// <c>Platee.Johann.slnx</c> turns up. The exe-existence check in <see cref="ExeLocator.Find"/>
/// itself needs a real build and is exercised only by actually running the UI suite.
/// </summary>
public sealed class ExeLocatorTests : IDisposable
{
    private readonly string root = Path.Combine(Path.GetTempPath(), "johann-exe-locator-tests", Guid.NewGuid().ToString("N"));

    public ExeLocatorTests() => Directory.CreateDirectory(this.root);

    public void Dispose()
    {
        try
        {
            Directory.Delete(this.root, recursive: true);
        }
        catch (Exception)
        {
            // Best-effort cleanup only.
        }
    }

    [Fact]
    public void FindSolutionRoot_finds_the_slnx_in_the_start_directory_itself()
    {
        File.WriteAllText(Path.Combine(this.root, "Platee.Johann.slnx"), "<Solution/>");

        ExeLocator.FindSolutionRoot(this.root).Should().Be(this.root);
    }

    [Fact]
    public void FindSolutionRoot_walks_up_through_several_ancestors()
    {
        File.WriteAllText(Path.Combine(this.root, "Platee.Johann.slnx"), "<Solution/>");
        var deep = Path.Combine(this.root, "Platee.Johann.UiTests", "bin", "Debug", "net10.0-windows");
        Directory.CreateDirectory(deep);

        ExeLocator.FindSolutionRoot(deep).Should().Be(this.root);
    }

    [Fact]
    public void FindSolutionRoot_returns_null_when_no_ancestor_has_the_slnx()
    {
        // this.root's ancestors are real OS/temp directories that do not contain the slnx —
        // unless this test happens to run from inside the repo's own temp tree, which it does not.
        ExeLocator.FindSolutionRoot(this.root).Should().BeNull();
    }

    [Fact]
    public void Find_throws_a_clear_message_when_the_slnx_is_missing()
    {
        var act = () => ExeLocator.Find(this.root);

        act.Should().Throw<InvalidOperationException>().WithMessage("*Platee.Johann.slnx*");
    }

    [Fact]
    public void Find_throws_a_build_hint_when_the_slnx_exists_but_the_exe_was_never_built()
    {
        File.WriteAllText(Path.Combine(this.root, "Platee.Johann.slnx"), "<Solution/>");

        var act = () => ExeLocator.Find(this.root);

        act.Should().Throw<FileNotFoundException>().WithMessage("*dotnet build*");
    }
}
