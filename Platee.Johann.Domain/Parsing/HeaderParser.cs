using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;

namespace Platee.Johann.Domain.Parsing;

/// <summary>
/// Parses the first words of a transcript to extract Type, ProjectName, and optional Title.
///
/// Rules:
///   Word 1: If a known type keyword → Type; cursor advances.
///           Otherwise → Type=Projekt, Legacy resolver used for project.
///   Word 2: ProjectName (when type was explicit).
///   Word 3: "Titel" or "Betreff" → title up to 15 words until "Ende".
/// </summary>
public sealed class HeaderParser
{
    private static readonly char[] PunctuationChars = [' ', ',', '.', ':', ';', '!', '?'];

    public ParsedHeader Parse(string transcript)
    {
        if (string.IsNullOrWhiteSpace(transcript))
            return new ParsedHeader(EntryType.Projekt, LegacyProjectResolver.Fallback, null, transcript);

        var words = transcript.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        if (words.Length == 0)
            return new ParsedHeader(EntryType.Projekt, LegacyProjectResolver.Fallback, null, transcript);

        // --- Step 1: Detect type from first word ---
        var detectedType = TypeExtractor.TryExtract(words[0]);
        bool typeIsExplicit = detectedType.HasValue;
        var type = detectedType ?? EntryType.Projekt;
        int cursor = typeIsExplicit ? 1 : 0;

        // --- Step 2: Project name ---
        string project;
        if (typeIsExplicit && cursor < words.Length)
        {
            project = words[cursor++].Trim(PunctuationChars);
        }
        else
        {
            project = LegacyProjectResolver.Resolve(transcript);
            // cursor stays at 0 — the whole transcript is remainder
        }

        // --- Step 3: Optional title ---
        string? explicitTitle = TitleExtractor.TryExtract(words, ref cursor);

        // --- Remainder ---
        string remainder = cursor < words.Length
            ? string.Join(" ", words[cursor..])
            : string.Empty;

        return new ParsedHeader(type, project, explicitTitle, remainder);
    }
}
