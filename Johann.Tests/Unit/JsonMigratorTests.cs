using System.Text.Json;
using FluentAssertions;
using Johann.Infrastructure.Json;

namespace Johann.Tests.Unit;

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

        dto.SchemaVersion.Should().Be(2);
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
        dto.SchemaVersion.Should().Be(2);
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
}
