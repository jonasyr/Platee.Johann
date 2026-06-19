namespace Platee.Johann.Tests.Unit;

using System.Text.Json;
using FluentAssertions;
using Platee.Johann.Infrastructure.Json;

public sealed class JsonMigratorTests
{
    // --- v2 documents pass through unchanged ---
    [Fact]
    public void Migrate_V2Document_PreservesAllFields()
    {
        var json = """
            {
              "schemaVersion": 2,
              "jobId": "260227_001_Johann_abc",
              "sequenceNumber": 1,
              "type": "Aufgabe",
              "projectName": "Johann",
              "title": "Test Titel",
              "createdAt": "2026-02-27T14:00:00+01:00",
              "sourceType": "audio",
              "durationSeconds": 45.0,
              "wordCount": 100,
              "status": { "transcribed": true, "summarized": true,
                          "pdfCreated": false, "archived": false, "emailCreated": false },
              "taskList": "1. Task A"
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;
        var dto = JsonMigrator.Migrate(element);

        dto.SchemaVersion.Should().Be(3);
        dto.Type.Should().Be("Aufgabe");
        dto.Title.Should().Be("Test Titel");
        dto.TaskList.Should().Be("1. Task A");
    }

    // --- v1 (Python-generated) documents get migrated ---
    [Fact]
    public void Migrate_V1WithoutTypeField_DefaultsToProjekt()
    {
        var json = """
            {
              "jobId": "2026-02-27_001_test_abc",
              "sequenceNumber": 1,
              "projectName": "TestProjekt",
              "createdAt": "2026-02-27T14:00:00+01:00",
              "sourceType": "mp3",
              "durationSeconds": 30.0,
              "wordCount": 50,
              "status": { "transcribed": true, "summarized": true,
                          "pdfCreated": false, "archived": false, "emailCreated": false },
              "longSummary": "### Hauptpunkte\nErste Zeile\nZweite Zeile"
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;
        var dto = JsonMigrator.Migrate(element);

        dto.Type.Should().Be("Projekt");
        dto.SchemaVersion.Should().Be(3);
    }

    [Fact]
    public void Migrate_V1WithMp3SourceType_MigratedToAudio()
    {
        var json = """
            {
              "jobId": "test",
              "sequenceNumber": 1,
              "projectName": "Test",
              "createdAt": "2026-02-27T14:00:00+01:00",
              "sourceType": "mp3",
              "wordCount": 10,
              "status": { "transcribed": false, "summarized": false,
                          "pdfCreated": false, "archived": false, "emailCreated": false }
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;
        var dto = JsonMigrator.Migrate(element);

        dto.SourceType.Should().Be("audio");
    }

    [Fact]
    public void Migrate_V1WithoutTitle_DerivesTitleFromLongSummary()
    {
        var json = """
            {
              "jobId": "test",
              "sequenceNumber": 1,
              "projectName": "Test",
              "createdAt": "2026-02-27T14:00:00+01:00",
              "sourceType": "text",
              "wordCount": 10,
              "longSummary": "### Kontext\nDas ist die erste inhaltliche Zeile",
              "status": { "transcribed": false, "summarized": true,
                          "pdfCreated": false, "archived": false, "emailCreated": false }
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;
        var dto = JsonMigrator.Migrate(element);

        dto.Title.Should().NotBeNullOrWhiteSpace();
        dto.Title.Should().Contain("erste inhaltliche Zeile");
    }

    // --- Python snake_case field mapping ---
    [Fact]
    public void Migrate_PythonSnakeCaseJson_MapsAllFieldsCorrectly()
    {
        // This is the exact format Python outputs
        var json = """
            {
              "job_id": "2026-02-10_120136_Audio_05_06c8e3",
              "sequence_number": 3,
              "source_type": "mp3",
              "created_at": "2026-02-12T10:26:43+01:00",
              "duration_seconds": 85.6,
              "word_count": 115,
              "project": "Iris",
              "abstract": "Kurze Zusammenfassung.",
              "long_summary": "### Hauptpunkte\nDetail A\nDetail B",
              "prose_summary": "Ausführlicher Fließtext hier.",
              "email_text": null,
              "transcript": "Original Transkript Text.",
              "status": {
                "transcribed": true,
                "summarized": true,
                "pdf_created": true,
                "archived": false,
                "email_created": false
              }
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;
        var dto = JsonMigrator.Migrate(element);

        dto.JobId.Should().Be("2026-02-10_120136_Audio_05_06c8e3");
        dto.SequenceNumber.Should().Be(3);
        dto.SourceType.Should().Be("audio");        // mp3 → audio
        dto.DurationSeconds.Should().BeApproximately(85.6, 0.01);
        dto.WordCount.Should().Be(115);
        dto.ProjectName.Should().Be("Iris");
        dto.Abstract.Should().Be("Kurze Zusammenfassung.");
        dto.LongSummary.Should().StartWith("### Hauptpunkte");
        dto.ProseSummary.Should().Be("Ausführlicher Fließtext hier.");
        dto.Transcript.Should().Be("Original Transkript Text.");
        dto.Status.PdfCreated.Should().BeTrue();
        dto.Status.EmailCreated.Should().BeFalse();
        dto.Type.Should().Be("Projekt");            // no type field → Projekt
        dto.SchemaVersion.Should().Be(3);
    }

    // --- v2-to-v3 migration ---
    [Fact]
    public void Migrate_V2WithoutEditedTranscript_DefaultsToNull_SetsVersion3()
    {
        var json = """
            {
              "schemaVersion": 2,
              "jobId": "260227_001_Test_abc",
              "sequenceNumber": 1,
              "type": "Projekt",
              "projectName": "Test",
              "title": "Test Titel",
              "createdAt": "2026-02-27T14:00:00+01:00",
              "sourceType": "audio",
              "durationSeconds": 45.0,
              "wordCount": 100,
              "transcript": "Original transcript text",
              "status": { "transcribed": true, "summarized": true,
                          "pdfCreated": false, "archived": false, "emailCreated": false }
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;
        var dto = JsonMigrator.Migrate(element);

        dto.SchemaVersion.Should().Be(3);
        dto.EditedTranscript.Should().BeNull();
        dto.Transcript.Should().Be("Original transcript text");
    }

    [Fact]
    public void Migrate_V3WithEditedTranscript_PreservesBoth()
    {
        var json = """
            {
              "schemaVersion": 3,
              "jobId": "260227_001_Test_abc",
              "sequenceNumber": 1,
              "type": "Projekt",
              "projectName": "Test",
              "title": "Test Titel",
              "createdAt": "2026-02-27T14:00:00+01:00",
              "sourceType": "audio",
              "durationSeconds": 45.0,
              "wordCount": 100,
              "transcript": "Original from Whisper",
              "editedTranscript": "User corrected version",
              "status": { "transcribed": true, "summarized": true,
                          "pdfCreated": false, "archived": false, "emailCreated": false }
            }
            """;
        var element = JsonDocument.Parse(json).RootElement;
        var dto = JsonMigrator.Migrate(element);

        dto.SchemaVersion.Should().Be(3);
        dto.Transcript.Should().Be("Original from Whisper");
        dto.EditedTranscript.Should().Be("User corrected version");
    }
}
