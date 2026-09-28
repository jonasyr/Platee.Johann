namespace Platee.Johann.Infrastructure.Llm;

using Platee.Johann.Application.Interfaces;

/// <summary>
/// Stub ohne API-Schlüssel, analog zu den übrigen No-Op-Adaptern.
/// <para>
/// Liefert <see cref="ModelProbeResult.NoApiKey"/> statt zu scheitern: ohne Schlüssel ist
/// keine Aussage möglich, und das darf das Speichern nicht verhindern.
/// </para>
/// </summary>
public sealed class NoOpModelAvailabilityProbe : IModelAvailabilityProbe
{
    /// <inheritdoc/>
    public Task<ModelProbeResult> ProbeAsync(string modelId, CancellationToken ct = default) =>
        Task.FromResult(ModelProbeResult.NoApiKey);
}
