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
    /// collide with a <see cref="BuiltInSections"/> id either, and the random suffix
    /// guarantees a deleted category's id is never handed out a second time.
    /// </summary>
    public static string Create(
        string name, IEnumerable<string> existingIds, Func<string>? suffixFactory = null)
    {
        var taken = new HashSet<string>(existingIds, StringComparer.OrdinalIgnoreCase);
        var baseId = Prefix + Slug(name);
        var suffix = suffixFactory ?? DefaultSuffix;

        // The suffix is what makes a deleted category's id unrecoverable. Without it,
        // deleting a category and creating another one frees the slug for reuse, and the
        // orphaned text of the old category silently re-attaches to the new one.
        for (var attempt = 0; attempt < 1000; attempt++)
        {
            var candidate = $"{baseId}-{suffix()}";
            if (!taken.Contains(candidate))
            {
                return candidate;
            }
        }

        return $"{baseId}-{Guid.NewGuid():N}";
    }

    private static string DefaultSuffix() => Random.Shared.Next(0x10000).ToString("x4");

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
