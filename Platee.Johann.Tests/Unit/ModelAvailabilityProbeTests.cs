namespace Platee.Johann.Tests.Unit;

using System.Net.Http;
using FluentAssertions;
using Platee.Johann.Application.Interfaces;
using Platee.Johann.Infrastructure.Llm;
using Xunit;

/// <summary>
/// #71 Stufe 2 — ein abgekuendigtes Modell soll beim Auswaehlen auffallen, nicht erst beim
/// naechsten Diktat.
/// <para>
/// Die tragende Unterscheidung ist <see cref="ModelProbeResult.NotFound"/> gegen
/// <see cref="ModelProbeResult.NetworkError"/>: wer ohne Verbindung die Einstellungen
/// oeffnet, muss trotzdem speichern koennen. Wuerde ein Netzwerkfehler wie ein fehlendes
/// Modell behandelt, waere der Nutzer im Zug ausgesperrt.
/// </para>
/// </summary>
public sealed class ModelAvailabilityProbeTests
{
    [Fact]
    public async Task Without_a_key_the_stub_reports_NoApiKey_rather_than_failing()
    {
        var probe = new NoOpModelAvailabilityProbe();

        (await probe.ProbeAsync("gpt-5.6-luna")).Should().Be(ModelProbeResult.NoApiKey);
    }

    [Fact]
    public async Task An_empty_key_is_NoApiKey_and_costs_no_request()
    {
        using var probe = new OpenAiModelAvailabilityProbe(apiKey: string.Empty);

        (await probe.ProbeAsync("gpt-5.6-luna")).Should().Be(ModelProbeResult.NoApiKey);
    }

    [Fact]
    public async Task An_unreachable_host_is_a_network_error_not_a_missing_model()
    {
        using var probe = new OpenAiModelAvailabilityProbe(
            apiKey: "sk-test",
            baseAddress: new Uri("https://localhost:9/"));

        (await probe.ProbeAsync("gpt-5.6-luna")).Should().Be(ModelProbeResult.NetworkError);
    }

    [Fact]
    public async Task A_404_means_the_model_is_gone()
    {
        using var probe = new OpenAiModelAvailabilityProbe(
            apiKey: "sk-test",
            handler: new StubHandler(System.Net.HttpStatusCode.NotFound));

        (await probe.ProbeAsync("gpt-5-nano")).Should().Be(ModelProbeResult.NotFound);
    }

    [Fact]
    public async Task A_200_means_the_model_is_there()
    {
        using var probe = new OpenAiModelAvailabilityProbe(
            apiKey: "sk-test",
            handler: new StubHandler(System.Net.HttpStatusCode.OK));

        (await probe.ProbeAsync("gpt-5.6-luna")).Should().Be(ModelProbeResult.Available);
    }

    [Theory]
    [InlineData(System.Net.HttpStatusCode.Unauthorized)]
    [InlineData(System.Net.HttpStatusCode.TooManyRequests)]
    [InlineData(System.Net.HttpStatusCode.InternalServerError)]
    public async Task Any_other_status_is_treated_as_unchecked_not_as_missing(
        System.Net.HttpStatusCode status)
    {
        // Falscher Schluessel, Drosselung, Serverfehler — alles Gruende, die nichts darueber
        // sagen, ob das Modell existiert. Lieber einmal zu viel speichern lassen als den
        // Nutzer aussperren.
        using var probe = new OpenAiModelAvailabilityProbe(
            apiKey: "sk-test",
            handler: new StubHandler(status));

        (await probe.ProbeAsync("gpt-5.6-luna")).Should().Be(ModelProbeResult.NetworkError);
    }

    [Fact]
    public async Task A_cancelled_probe_throws_instead_of_reporting_a_network_error()
    {
        // Ein Modellwechsel bricht die laufende Pruefung ab. Wuerde das als Netzwerkfehler
        // durchgehen, schriebe die abgebrochene Pruefung ihren Status in die Anzeige.
        using var probe = new OpenAiModelAvailabilityProbe(
            apiKey: "sk-test",
            handler: new StubHandler(System.Net.HttpStatusCode.OK, delay: TimeSpan.FromSeconds(5)));
        using var cts = new CancellationTokenSource();

        var call = probe.ProbeAsync("gpt-5.6-luna", cts.Token);
        await cts.CancelAsync();

        await FluentActions.Awaiting(() => call).Should().ThrowAsync<OperationCanceledException>();
    }

    private sealed class StubHandler(System.Net.HttpStatusCode status, TimeSpan? delay = null)
        : HttpMessageHandler
    {
        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request, CancellationToken cancellationToken)
        {
            if (delay is { } wait)
            {
                await Task.Delay(wait, cancellationToken);
            }

            return new HttpResponseMessage(status);
        }
    }
}
