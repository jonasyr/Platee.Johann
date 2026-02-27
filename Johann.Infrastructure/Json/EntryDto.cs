using System.Text.Json;
using System.Text.Json.Serialization;

namespace Johann.Infrastructure.Json;

/// <summary>
/// JSON Data Transfer Object for persisting/loading Entry records.
/// Matches both v1 (Python-generated) and v2 (C#-generated) schema.
/// </summary>
public sealed class EntryDto
{
    [JsonPropertyName("schemaVersion")]
    public int SchemaVersion { get; set; } = 2;

    [JsonPropertyName("jobId")]
    public string JobId { get; set; } = string.Empty;

    [JsonPropertyName("sequenceNumber")]
    public int SequenceNumber { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = "Projekt";

    [JsonPropertyName("projectName")]
    public string ProjectName { get; set; } = "Allgemein";

    [JsonPropertyName("title")]
    public string Title { get; set; } = string.Empty;

    [JsonPropertyName("createdAt")]
    public DateTimeOffset CreatedAt { get; set; }

    [JsonPropertyName("sourceType")]
    public string SourceType { get; set; } = "text";

    [JsonPropertyName("durationSeconds")]
    public double DurationSeconds { get; set; }

    [JsonPropertyName("wordCount")]
    public int WordCount { get; set; }

    [JsonPropertyName("status")]
    public StatusDto Status { get; set; } = new();

    [JsonPropertyName("transcript")]
    public string? Transcript { get; set; }

    [JsonPropertyName("abstract")]
    public string? Abstract { get; set; }

    [JsonPropertyName("longSummary")]
    public string? LongSummary { get; set; }

    [JsonPropertyName("proseSummary")]
    public string? ProseSummary { get; set; }

    [JsonPropertyName("emailText")]
    public string? EmailText { get; set; }

    [JsonPropertyName("conversationNote")]
    public string? ConversationNote { get; set; }

    [JsonPropertyName("taskList")]
    public string? TaskList { get; set; }

    /// <summary>Extension bag for future fields without breaking schema changes.</summary>
    [JsonPropertyName("extensions")]
    public JsonElement? Extensions { get; set; }
}

public sealed class StatusDto
{
    [JsonPropertyName("transcribed")]
    public bool Transcribed { get; set; }

    [JsonPropertyName("summarized")]
    public bool Summarized { get; set; }

    [JsonPropertyName("pdfCreated")]
    public bool PdfCreated { get; set; }

    [JsonPropertyName("archived")]
    public bool Archived { get; set; }

    [JsonPropertyName("emailCreated")]
    public bool EmailCreated { get; set; }
}
