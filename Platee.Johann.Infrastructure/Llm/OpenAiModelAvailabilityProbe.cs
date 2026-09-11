namespace Platee.Johann.Infrastructure.Llm;

using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using Platee.Johann.Application.Interfaces;

/// <summary>
/// Fragt <c>GET /v1/models/{id}</c> ab. Der Aufruf kostet keine Token.
/// <para>
/// Jeder andere Statuscode als 200 oder 404 — etwa 401 bei falschem Schlüssel, 429 bei
/// Drosselung, 5xx bei einer Störung — wird bewusst zu
/// <see cref="ModelProbeResult.NetworkError"/> und damit <b>nicht</b> blockierend. Keiner
/// dieser Fälle sagt etwas darüber aus, ob das Modell existiert; lieber einmal zu viel
/// speichern lassen als den Nutzer aussperren.
/// </para>
/// </summary>
public sealed class OpenAiModelAvailabilityProbe : IModelAvailabilityProbe, IDisposable
{
    private readonly HttpClient http;
    private readonly string apiKey;

    /// <summary>Initializes a new instance of the <see cref="OpenAiModelAvailabilityProbe"/> class.</summary>
    /// <param name="apiKey">Der OpenAI-Schlüssel. Leer bedeutet „nicht prüfbar".</param>
    /// <param name="baseAddress">Abweichende Basisadresse, nur für Tests.</param>
    /// <param name="handler">Abweichender Nachrichten-Handler, nur für Tests.</param>
    public OpenAiModelAvailabilityProbe(
        string apiKey,
        Uri? baseAddress = null,
        HttpMessageHandler? handler = null)
    {
        this.apiKey = apiKey;
        this.http = handler is null ? new HttpClient() : new HttpClient(handler);
        this.http.BaseAddress = baseAddress ?? new Uri("https://api.openai.com/");
        this.http.Timeout = TimeSpan.FromSeconds(10);
    }

    /// <inheritdoc/>
    public async Task<ModelProbeResult> ProbeAsync(string modelId, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(this.apiKey) || string.IsNullOrWhiteSpace(modelId))
        {
            return ModelProbeResult.NoApiKey;
        }

        try
        {
            using var request = new HttpRequestMessage(
                HttpMethod.Get, $"v1/models/{Uri.EscapeDataString(modelId)}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", this.apiKey);

            using var response = await this.http.SendAsync(request, ct);

            return response.StatusCode switch
            {
                HttpStatusCode.OK => ModelProbeResult.Available,
                HttpStatusCode.NotFound => ModelProbeResult.NotFound,
                _ => ModelProbeResult.NetworkError,
            };
        }
        catch (OperationCanceledException) when (ct.IsCancellationRequested)
        {
            // Abbruch durch einen Modellwechsel ist kein Netzwerkfehler. Würde er als
            // solcher zurückkommen, schriebe die verworfene Prüfung ihren Status in die
            // Anzeige des inzwischen gewählten Modells.
            throw;
        }
        catch (Exception)
        {
            return ModelProbeResult.NetworkError;
        }
    }

    /// <inheritdoc/>
    public void Dispose() => this.http.Dispose();
}
