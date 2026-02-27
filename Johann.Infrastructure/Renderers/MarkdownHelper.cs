using Markdig;

namespace Johann.Infrastructure.Renderers;

/// <summary>
/// Converts Markdown to HTML using the Markdig pipeline.
/// Markdig handles XSS sanitization internally.
/// </summary>
internal static class MarkdownHelper
{
    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .Build();

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;
        return Markdown.ToHtml(markdown, Pipeline);
    }
}
