using Johann.Domain.Entities;

namespace Johann.Application.Interfaces;

public sealed record RenderResult(byte[] Data, string MimeType, string SuggestedFilename);
public sealed record RenderOptions(
    string? OutputDirectory = null,
    bool OpenAfterRender = false,
    bool IncludeTranscript = true);

public interface IEntryRenderer
{
    string RendererName { get; }
    Task<RenderResult> RenderAsync(Entry entry, RenderOptions options, CancellationToken ct = default);
}
