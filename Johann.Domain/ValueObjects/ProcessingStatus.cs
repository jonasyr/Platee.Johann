namespace Johann.Domain.ValueObjects;

public sealed record ProcessingStatus(
    bool Transcribed,
    bool Summarized,
    bool PdfCreated,
    bool Archived,
    bool EmailCreated)
{
    public static ProcessingStatus Empty => new(false, false, false, false, false);
}
