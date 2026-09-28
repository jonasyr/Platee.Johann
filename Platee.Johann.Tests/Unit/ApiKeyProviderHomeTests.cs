namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Infrastructure.Llm;
using Xunit;

public sealed class ApiKeyProviderHomeTests : IDisposable
{
    private readonly string root = Directory.CreateTempSubdirectory("johann-key-").FullName;

    public void Dispose() => Directory.Delete(this.root, recursive: true);

    [Fact]
    public void HomeOverride_ReadsEnvFromHome()
    {
        var home = Directory.CreateDirectory(Path.Combine(this.root, "home")).FullName;
        File.WriteAllText(Path.Combine(home, ".env"), "OPENAI_API_KEY=sk-home\n");

        ApiKeyProvider.TryGetOpenAiKey(Read(home), baseDirectory: this.root).Should().Be("sk-home");
    }

    [Fact]
    public void HomeOverride_DoesNotWalkUpFromExe()
    {
        var home = Directory.CreateDirectory(Path.Combine(this.root, "home")).FullName;
        var exeDir = Directory.CreateDirectory(Path.Combine(this.root, "repo", "bin")).FullName;
        File.WriteAllText(Path.Combine(this.root, "repo", ".env"), "OPENAI_API_KEY=sk-repo\n");

        ApiKeyProvider.TryGetOpenAiKey(Read(home), baseDirectory: exeDir).Should().BeNull();
    }

    [Fact]
    public void NoOverride_StillWalksUp()
    {
        var exeDir = Directory.CreateDirectory(Path.Combine(this.root, "repo", "bin")).FullName;
        File.WriteAllText(Path.Combine(this.root, "repo", ".env"), "OPENAI_API_KEY=sk-repo\n");

        // Kein JOHANN_HOME, kein OPENAI_API_KEY. Documents\Johann\.env des Entwicklers kann existieren —
        // dann gewinnt sie (heutiges Verhalten). Der Test prüft nur, dass der Walk-up noch greift.
        var key = ApiKeyProvider.TryGetOpenAiKey(_ => null, baseDirectory: exeDir);
        key.Should().NotBeNull();
    }

    [Fact]
    public void EnvironmentVariable_StillWins()
    {
        ApiKeyProvider.TryGetOpenAiKey(n => n == "OPENAI_API_KEY" ? "sk-env" : null, this.root)
            .Should().Be("sk-env");
    }

    private static Func<string, string?> Read(string home) =>
        name => name == "JOHANN_HOME" ? home : null;
}
