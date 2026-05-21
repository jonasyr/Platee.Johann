namespace Platee.Johann.Infrastructure.Renderers;

using Markdig;

/// <summary>
/// Converts Markdown to HTML using a Markdig pipeline with raw HTML disabled.
/// </summary>
internal static class MarkdownHelper
{
    private static readonly MarkdownPipeline Pipeline =
        new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .DisableHtml()
            .Build();

    public static string ToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return string.Empty;
        }

        return Markdown.ToHtml(markdown, Pipeline);
    }
}
