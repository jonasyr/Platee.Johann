namespace Platee.Johann.Application.Interfaces;

using Platee.Johann.Application.Processing;
using Platee.Johann.Domain.Entities;

public sealed record RenderResult(byte[] Data, string MimeType, string SuggestedFilename);

public sealed record RenderOptions(
    string? OutputDirectory = null,
    bool OpenAfterRender = false,
    bool IncludeTranscript = true,
    SectionVisibility? Sections = null)
{
    /// <summary>Gets the category id → display name map, for rendering custom section headings.</summary>
    /// <remarks>
    /// Kept off the positional parameter list on purpose: <see cref="SectionVisibility"/> and the
    /// three renderers consume the positional shape, so extending it multiplies the diff.
    /// Defaults to empty, so every existing call site compiles and behaves unchanged.
    /// </remarks>
    public IReadOnlyDictionary<string, string> CustomSectionNames { get; init; }
        = new Dictionary<string, string>();

    /// <summary>Gets the category id → include-in-output map. A missing key means visible.</summary>
    public IReadOnlyDictionary<string, bool> CustomSectionVisibility { get; init; }
        = new Dictionary<string, bool>();

    /// <summary>
    /// Selects the custom sections of <paramref name="entry"/> that belong in the output,
    /// in a stable id order so repeated renders of the same entry produce identical files.
    /// </summary>
    /// <param name="entry">The entry whose custom sections are being rendered.</param>
    /// <returns>Heading/body pairs, blank and hidden sections already removed.</returns>
    public IEnumerable<(string Id, string Heading, string Text)> SelectCustomSections(Entry entry)
    {
        foreach (var pair in entry.CustomSections.OrderBy(p => p.Key, StringComparer.Ordinal))
        {
            if (this.CustomSectionVisibility.TryGetValue(pair.Key, out var visible) && !visible)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(pair.Value))
            {
                continue;
            }

            var heading = this.CustomSectionNames.TryGetValue(pair.Key, out var name)
                          && !string.IsNullOrWhiteSpace(name)
                ? name
                : pair.Key;

            yield return (pair.Key, heading, pair.Value);
        }
    }
}

public interface IEntryRenderer
{
    string RendererName { get; }

    Task<RenderResult> RenderAsync(Entry entry, RenderOptions options, CancellationToken ct = default);
}
