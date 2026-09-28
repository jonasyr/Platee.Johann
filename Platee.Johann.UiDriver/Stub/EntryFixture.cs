namespace Platee.Johann.UiDriver.Stub;

using System.Text.Json.Nodes;

public sealed record EntryFixture(string Transcript, string Title, IReadOnlyDictionary<string, string> Sections)
{
    public static readonly IReadOnlyList<string> SectionKeys =
        ["abstract", "longSummary", "proseSummary", "emailText", "conversationNote", "taskList", "stundenzettelText", "analogText"];

    public static EntryFixture Load(string statusJsonPath)
    {
        var node = JsonNode.Parse(File.ReadAllText(statusJsonPath))!;
        var sections = SectionKeys
            .Select(k => (k, v: node[k]?.GetValue<string>()))
            .Where(p => !string.IsNullOrEmpty(p.v))
            .ToDictionary(p => p.k, p => p.v!);
        return new EntryFixture(
            node["transcript"]?.GetValue<string>() ?? string.Empty,
            node["title"]?.GetValue<string>() ?? string.Empty,
            sections);
    }
}
