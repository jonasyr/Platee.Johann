using Platee.Johann.Domain.Enums;

namespace Platee.Johann.Domain.Parsing;

public static class TypeExtractor
{
    private static readonly Dictionary<string, EntryType> Keywords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["aufgabe"] = EntryType.Aufgabe,
            ["analog"] = EntryType.Analog,
            ["email"] = EntryType.EMail,
            ["e-mail"] = EntryType.EMail,
            ["gesprächsnotiz"] = EntryType.Gesprächsnotiz,
            ["gesprächsnotizen"] = EntryType.Gesprächsnotiz,
            ["stundenzettel"] = EntryType.Stundenzettel,
            ["projekt"] = EntryType.Projekt,
        };

    private static readonly char[] PunctuationChars = [' ', ',', '.', ':', ';', '!', '?'];

    /// <summary>Returns the EntryType if the word is a known keyword, otherwise null.</summary>
    public static EntryType? TryExtract(string word)
    {
        var cleanWord = word.Trim(PunctuationChars);
        return Keywords.TryGetValue(cleanWord, out var t) ? t : null;
    }

    public static bool IsTypeKeyword(string word)
    {
        var cleanWord = word.Trim(PunctuationChars);
        return Keywords.ContainsKey(cleanWord);
    }
}
