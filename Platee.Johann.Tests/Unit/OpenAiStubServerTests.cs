namespace Platee.Johann.Tests.Unit;

using System.Net;
using System.Net.Http;
using System.Text;
using FluentAssertions;
using Platee.Johann.UiDriver.Stub;
using Xunit;

public sealed class OpenAiStubServerTests
{
    private static StringContent Chat(string user) => new(
        $$"""{"model":"gpt-5.6-luna","messages":[{"role":"system","content":"sys"},{"role":"user","content":{{System.Text.Json.JsonSerializer.Serialize(user)}}}]}""",
        Encoding.UTF8, "application/json");

    [Fact]
    public async Task Chat_UsesResponder_AndLogsRequest()
    {
        using var stub = OpenAiStubServer.Start();
        stub.OnChat(user => user.StartsWith("Titel") ? "Mein Titel" : null);
        using var http = new HttpClient { BaseAddress = stub.Root };

        var response = await http.PostAsync("v1/chat/completions", Chat("Titel bitte"));

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        (await response.Content.ReadAsStringAsync()).Should().Contain("\"content\":\"Mein Titel\"");
        stub.Requests.Should().ContainSingle(r => r.Path == "/v1/chat/completions");
    }

    [Fact]
    public async Task Chat_WithoutMatchingRule_Returns500_NamingTheRequest()
    {
        using var stub = OpenAiStubServer.Start();
        using var http = new HttpClient { BaseAddress = stub.Root };

        var response = await http.PostAsync("v1/chat/completions", Chat("unbekannt"));

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await response.Content.ReadAsStringAsync()).Should().Contain("kein Stub für");
    }

    [Fact]
    public async Task FailNext_AppliesOnce()
    {
        using var stub = OpenAiStubServer.Start();
        stub.OnChat(_ => "ok");
        stub.FailNext("/v1/chat/completions", 500);
        using var http = new HttpClient { BaseAddress = stub.Root };

        (await http.PostAsync("v1/chat/completions", Chat("a"))).StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        (await http.PostAsync("v1/chat/completions", Chat("a"))).StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Models_ExistUnlessMarkedMissing()
    {
        using var stub = OpenAiStubServer.Start();
        stub.MissingModel("gpt-5.6-sol");
        using var http = new HttpClient { BaseAddress = stub.Root };

        (await http.GetAsync("v1/models/gpt-5.6-luna")).StatusCode.Should().Be(HttpStatusCode.OK);
        (await http.GetAsync("v1/models/gpt-5.6-sol")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MalformedChatBody_Returns500_AndRecordsError()
    {
        using var stub = OpenAiStubServer.Start();
        using var http = new HttpClient { BaseAddress = stub.Root };
        using var malformed = new StringContent("{kaputt", Encoding.UTF8, "application/json");

        var response = await http.PostAsync("v1/chat/completions", malformed);

        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
        stub.Errors.Should().ContainSingle();
    }

    [Fact]
    public async Task Dispose_WhileRequestHanging_CompletesQuickly_AndRecordsNoError()
    {
        var stub = OpenAiStubServer.Start();
        stub.Hang("/v1/chat/completions");
        using var http = new HttpClient { BaseAddress = stub.Root };
        _ = http.PostAsync("v1/chat/completions", Chat("a"));
        await Task.Delay(100);

        var sw = System.Diagnostics.Stopwatch.StartNew();
        stub.Dispose();
        sw.Stop();

        sw.Elapsed.Should().BeLessThan(TimeSpan.FromSeconds(3));
        stub.Errors.Should().BeEmpty();
    }

    [Fact]
    public void EntryFixture_ReadsSectionsFromStatusJson()
    {
        var path = Path.Combine(Directory.CreateTempSubdirectory("fx-").FullName, "x_status.json");
        File.WriteAllText(path, """{"title":"T","transcript":"Hallo","taskList":"- a","abstract":"kurz"}""");

        var fx = EntryFixture.Load(path);

        fx.Title.Should().Be("T");
        fx.Transcript.Should().Be("Hallo");
        fx.Sections.Should().Contain("taskList", "- a").And.Contain("abstract", "kurz");
    }
}
