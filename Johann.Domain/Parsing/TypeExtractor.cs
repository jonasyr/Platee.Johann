using Johann.Domain.Enums;

namespace Johann.Domain.Parsing;

public static class TypeExtractor
{
    private static readonly Dictionary<string, EntryType> Keywords =
        new(StringComparer.OrdinalIgnoreCase)
        {
            ["aufgabe"]         = EntryType.Aufgabe,
            ["email"]           = EntryType.EMail,
            ["e-mail"]          = EntryType.EMail,
            ["gesprächsnotiz"]  = EntryType.Gesprächsnotiz,
            ["gesprächsnotizen"]= EntryType.Gesprächsnotiz,
            ["stundenzettel"]   = EntryType.Stundenzettel,
            ["projekt"]         = EntryType.Projekt,
        };

    /// <summary>Returns the EntryType if the word is a known keyword, otherwise null.</summary>
    public static EntryType? TryExtract(string word) =>
        Keywords.TryGetValue(word, out var t) ? t : null;

    public static bool IsTypeKeyword(string word) => Keywords.ContainsKey(word);
}
