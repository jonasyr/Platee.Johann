namespace Platee.Johann.Application.Settings;

using System.Text;

/// <summary>
/// Mints stable identifiers for user-created categories.
/// <para>
/// An id is minted once at creation and never re-derived from the category's name.
/// Generated text is persisted under the id in <c>Entry.CustomSections</c>, so deriving it
/// from the mutable name would orphan every existing section the moment a user renames a
/// category.
/// </para>
/// </summary>
public static class CategoryIdFactory
{
    private const string Prefix = "custom.";

    private const string FallbackSlug = "kategorie";

    /// <summary>
    /// Creates an id for <paramref name="name"/> that does not collide with any of
    /// <paramref name="existingIds"/>. The <c>custom.</c> prefix guarantees it can never
    /// collide with a <see cref="BuiltInSections"/> id either.
    /// </summary>
    public static string Create(string name, IEnumerable<string> existingIds)
    {
        var taken = new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);
        var baseId = Prefix + Slug(name);

        if (!taken.Contains(baseId))
        {
            return baseId;
        }

        for (var i = 2; ; i++)
        {
            var candidate = $"{baseId}-{i}";
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }
    }

    private static string Slug(string name)
    {
        var expanded = name
            .Replace("ä", "ae", StringComparison.OrdinalIgnoreCase)
            .Replace("ö", "oe", StringComparison.OrdinalIgnoreCase)
            .Replace("ü", "ue", StringComparison.OrdinalIgnoreCase)
            .Replace("ß", "ss", StringComparison.OrdinalIgnoreCase);

        var sb = new StringBuilder();
        var pendingSeparator = false;

        foreach (var ch in expanded.ToLowerInvariant())
        {
            if (char.IsAsciiLetterOrDigit(ch))
            {
                if (pendingSeparator && sb.Length > 0)
                {
                    sb.Append('-');
                }

                sb.Append(ch);
                pendingSeparator = false;
            }
            else
            {
                // Defer the separator so runs of punctuation collapse into one dash and
                // trailing punctuation never produces a dangling dash.
                pendingSeparator = true;
            }
        }

        var slug = sb.ToString();
        return slug.Length == 0 ? FallbackSlug : slug;
    }
}
