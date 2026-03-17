using Platee.Johann.Domain.Enums;

namespace Platee.Johann.Domain.ValueObjects;

/// <summary>
/// Result of parsing the first words of a transcript for type/project/title.
/// </summary>
public sealed record ParsedHeader(
    EntryType Type,
    string ProjectName,
    string? ExplicitTitle,   // null = TitleGenerator will be used
    string RemainderText     // Transcript text after header tokens
);
