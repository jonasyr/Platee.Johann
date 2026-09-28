namespace Platee.Johann.UiTests;

using UglyToad.PdfPig;

/// <summary>
/// Reads the visible text of a PDF Johann rendered, for assertions on what a user would read in
/// it. Words are joined with single spaces (page by page), so a phrase that wraps across a line
/// in the PDF is still found as one phrase — <c>Page.Text</c> glues the letters of neighbouring
/// lines together without a separator.
/// </summary>
public static class PdfText
{
    public static string Extract(string pdfPath)
    {
        using var document = PdfDocument.Open(pdfPath);
        return string.Join(
            " ",
            document.GetPages().SelectMany(page => page.GetWords()).Select(word => word.Text));
    }
}
