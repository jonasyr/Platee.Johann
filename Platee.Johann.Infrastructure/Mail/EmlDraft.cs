namespace Platee.Johann.Infrastructure.Mail;

using System.Globalization;
using System.Text;

/// <summary>
/// Writes an unsent mail as an .eml file (RFC 5322 / MIME) for the new Outlook for Windows,
/// which has no COM object model but opens such a file as an editable draft — with attachments,
/// and it adds the user's signature itself (#57).
/// <para>
/// Two headers carry the behaviour: <c>X-Unsent: 1</c> makes Outlook open a draft instead of a
/// read-only message, and a <c>Message-ID</c> of our own is required since Outlook 1.2026.105,
/// which no longer assigns one when saving the draft.
/// </para>
/// </summary>
public static class EmlDraft
{
    private const string Crlf = "\r\n";
    /// <summary>39 bytes → 52 base64 characters; with "Subject: " the line stays below 78.</summary>
    private const int MaxEncodedBytesPerWord = 39;

    /// <summary>Builds the .eml text.</summary>
    /// <param name="subject">Subject line.</param>
    /// <param name="htmlFragment">Body HTML; wrapped into a complete UTF-8 document.</param>
    /// <param name="attachments">File name and content of each attachment.</param>
    /// <param name="date">Date header.</param>
    /// <param name="id">Unique id for Message-ID and MIME boundary.</param>
    /// <returns>The file content, CRLF line endings, ASCII only.</returns>
    public static string Build(
        string subject,
        string htmlFragment,
        IReadOnlyList<(string Name, byte[] Content)> attachments,
        DateTimeOffset date,
        Guid id)
    {
        var boundary = $"----=_Johann_{id:N}";
        var sb = new StringBuilder();
        sb.Append("X-Unsent: 1").Append(Crlf);
        sb.Append($"Message-ID: <{id:N}@johann.local>").Append(Crlf);
        sb.Append("Date: ").Append(FormatDate(date)).Append(Crlf);
        sb.Append("Subject: ").Append(EncodeHeader(subject)).Append(Crlf);
        sb.Append("MIME-Version: 1.0").Append(Crlf);
        sb.Append("Content-Type: multipart/mixed;").Append(Crlf).Append($" boundary=\"{boundary}\"").Append(Crlf);
        sb.Append(Crlf);

        var html = $"<html><head><meta charset=\"utf-8\"></head><body>{htmlFragment}</body></html>";
        sb.Append("--").Append(boundary).Append(Crlf);
        sb.Append("Content-Type: text/html; charset=utf-8").Append(Crlf);
        sb.Append("Content-Transfer-Encoding: base64").Append(Crlf).Append(Crlf);
        AppendBase64(sb, Encoding.UTF8.GetBytes(html));

        foreach (var (name, content) in attachments)
        {
            var encodedName = Uri.EscapeDataString(name);
            sb.Append("--").Append(boundary).Append(Crlf);
            sb.Append($"Content-Type: {ContentTypeOf(name)}; name=\"{EncodeHeader(name)}\"").Append(Crlf);
            sb.Append($"Content-Disposition: attachment; filename*=utf-8''{encodedName}").Append(Crlf);
            sb.Append("Content-Transfer-Encoding: base64").Append(Crlf).Append(Crlf);
            AppendBase64(sb, content);
        }

        sb.Append("--").Append(boundary).Append("--").Append(Crlf);
        return sb.ToString();
    }

    private static string FormatDate(DateTimeOffset date)
    {
        var offset = date.Offset;
        var sign = offset < TimeSpan.Zero ? '-' : '+';
        return date.ToString("ddd, dd MMM yyyy HH:mm:ss ", CultureInfo.InvariantCulture)
               + $"{sign}{Math.Abs(offset.Hours):00}{Math.Abs(offset.Minutes):00}";
    }

    /// <summary>
    /// RFC 2047 encoded words, split so no header line exceeds 78 characters and no UTF-8
    /// sequence is cut in half.
    /// </summary>
    private static string EncodeHeader(string value)
    {
        var words = new List<string>();
        var chunk = new StringBuilder();
        var chunkBytes = 0;
        foreach (var rune in value.EnumerateRunes())
        {
            var bytes = rune.Utf8SequenceLength;
            if (chunkBytes + bytes > MaxEncodedBytesPerWord)
            {
                words.Add(Word(chunk.ToString()));
                chunk.Clear();
                chunkBytes = 0;
            }

            chunk.Append(rune.ToString());
            chunkBytes += bytes;
        }

        words.Add(Word(chunk.ToString()));
        return string.Join(Crlf + " ", words);

        static string Word(string text) => $"=?utf-8?B?{Convert.ToBase64String(Encoding.UTF8.GetBytes(text))}?=";
    }

    private static void AppendBase64(StringBuilder sb, byte[] content)
    {
        var base64 = Convert.ToBase64String(content);
        for (var i = 0; i < base64.Length; i += 76)
        {
            sb.Append(base64, i, Math.Min(76, base64.Length - i)).Append(Crlf);
        }
    }

    private static string ContentTypeOf(string fileName)
        => Path.GetExtension(fileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase)
            ? "application/pdf"
            : "application/octet-stream";
}
