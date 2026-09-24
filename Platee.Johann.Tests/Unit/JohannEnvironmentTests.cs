namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Infrastructure.Hosting;
using Xunit;

public sealed class JohannEnvironmentTests
{
    private static Func<string, string?> Vars(params (string Name, string Value)[] vars) =>
        name => vars.FirstOrDefault(v => v.Name == name).Value;

    private static readonly string DefaultHome = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "Johann");

    [Fact]
    public void HomeDirectory_Unset_IsDocumentsJohann()
    {
        JohannEnvironment.HomeDirectory(Vars()).Should().Be(DefaultHome);
        JohannEnvironment.HasHomeOverride(Vars()).Should().BeFalse();
    }

    [Fact]
    public void HomeDirectory_Blank_IsDocumentsJohann()
    {
        JohannEnvironment.HomeDirectory(Vars(("JOHANN_HOME", "   "))).Should().Be(DefaultHome);
    }

    [Fact]
    public void HomeDirectory_Absolute_IsUsed()
    {
        JohannEnvironment.HomeDirectory(Vars(("JOHANN_HOME", @"C:\sandbox\home")))
            .Should().Be(@"C:\sandbox\home");
        JohannEnvironment.HasHomeOverride(Vars(("JOHANN_HOME", @"C:\sandbox\home"))).Should().BeTrue();
    }

    [Theory]
    [InlineData("\"C:\\sandbox\\home\\\"")]
    [InlineData(" C:\\sandbox\\home\\ ")]
    [InlineData("C:/sandbox/home")]
    public void HomeDirectory_QuotedOrTrailingSlash_IsNormalised(string raw)
    {
        JohannEnvironment.HomeDirectory(Vars(("JOHANN_HOME", raw))).Should().Be(@"C:\sandbox\home");
    }

    [Theory]
    [InlineData("sandbox\\home")]
    [InlineData("..\\home")]
    [InlineData("\\\\?\\")]
    public void HomeDirectory_Invalid_Throws_NamingTheVariable(string raw)
    {
        var act = () => JohannEnvironment.HomeDirectory(Vars(("JOHANN_HOME", raw)));
        act.Should().Throw<InvalidOperationException>().WithMessage("*JOHANN_HOME*");
    }

    [Fact]
    public void OpenAiRoot_Unset_IsNull()
    {
        JohannEnvironment.OpenAiRoot(Vars()).Should().BeNull();
    }

    [Theory]
    [InlineData("http://localhost:5123", "http://localhost:5123/")]
    [InlineData("http://localhost:5123/", "http://localhost:5123/")]
    [InlineData("https://proxy.example/openai", "https://proxy.example/openai/")]
    public void OpenAiRoot_Valid_EndsWithSlash(string raw, string expected)
    {
        JohannEnvironment.OpenAiRoot(Vars(("JOHANN_OPENAI_ENDPOINT", raw)))!.AbsoluteUri.Should().Be(expected);
    }

    [Theory]
    [InlineData("localhost:5123")]
    [InlineData("ftp://localhost/")]
    [InlineData("nicht eine url")]
    public void OpenAiRoot_Invalid_Throws_NamingTheVariable(string raw)
    {
        var act = () => JohannEnvironment.OpenAiRoot(Vars(("JOHANN_OPENAI_ENDPOINT", raw)));
        act.Should().Throw<InvalidOperationException>().WithMessage("*JOHANN_OPENAI_ENDPOINT*");
    }

    [Theory]
    [InlineData(null, false)]
    [InlineData("", false)]
    [InlineData("0", false)]
    [InlineData("1", true)]
    [InlineData("true", true)]
    [InlineData("TRUE", true)]
    public void SkipUpdateCheck(string? raw, bool expected)
    {
        JohannEnvironment.SkipUpdateCheck(name => name == "JOHANN_NO_UPDATE_CHECK" ? raw : null)
            .Should().Be(expected);
    }
}
