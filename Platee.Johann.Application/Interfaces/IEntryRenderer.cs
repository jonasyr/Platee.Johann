namespace Platee.Johann.Application.Interfaces;

using Platee.Johann.Application.Processing;
using Platee.Johann.Domain.Entities;

public sealed record RenderResult(byte[] Data, string MimeType, string SuggestedFilename);

public sealed record RenderOptions(
    string? OutputDirectory = null,
    bool OpenAfterRender = false,
    bool IncludeTranscript = true,
    SectionVisibility? Sections = null);

public interface IEntryRenderer
{
    string RendererName { get; }

    Task<RenderResult> RenderAsync(Entry entry, RenderOptions options, CancellationToken ct = default);
}
