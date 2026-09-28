namespace Platee.Johann.Tests.Unit;

using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Infrastructure.Llm;
using Platee.Johann.UiDriver.Stub;
using Xunit;

public sealed class OpenAiEndpointOverrideTests
{
    [Fact]
    public async Task LlmProvider_SendsToOverriddenRoot()
    {
        using var stub = OpenAiStubServer.Start();
        stub.OnChat(_ => "vom Stub");
        var provider = new OpenAiLlmProvider("sk-stub", stub.Root);

        var text = await provider.GenerateAsync("sys", "user", new LlmOptions(100, false, "gpt-5.6-luna"));

        text.Should().Be("vom Stub");
        stub.Requests.Should().ContainSingle(r => r.Path == "/v1/chat/completions");
    }

    [Fact]
    public async Task Transcriber_SendsToOverriddenRoot()
    {
        using var stub = OpenAiStubServer.Start();
        stub.OnTranscription(name => name == "d1.mp3" ? "Hallo Welt" : null);
        var dir = Directory.CreateTempSubdirectory("tr-").FullName;
        var mp3 = Path.Combine(dir, "d1.mp3");
        File.WriteAllBytes(mp3, new byte[1024]);   // Inhalt egal, Dauer-Leser wirft nie

        var result = await new WhisperTranscriber("sk-stub", stub.Root).TranscribeAsync(mp3);

        result.Transcript.Should().Be("Hallo Welt");
        stub.Requests.Should().ContainSingle(r => r.Path == "/v1/audio/transcriptions");
    }
}
