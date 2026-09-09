namespace Platee.Johann.Tests.Unit;

using System.Text.Json;
using FluentAssertions;
using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;
using Platee.Johann.Infrastructure.Json;

/// <summary>
/// Covers schema v4: the additive <c>customSections</c> map, the round-trip through the
/// public <see cref="JsonRepository"/> seam, and the two <see cref="JsonMigrator"/> bugs
/// this change fixes (unconditional version downgrade, lossy unknown fields).
/// </summary>
public sealed class CustomSectionPersistenceTests : IDisposable
{
    private static readonly string V4Json = """
        {
          "schemaVersion": 4,
          "jobId": "260907_001_abc",
          "sequenceNumber": 1,
          "type": "Projekt",
          "projectName": "Johann",
          "title": "Test",
          "createdAt": "2026-09-07T14:00:00+01:00",
          "sourceType": "audio",
          "durationSeconds": 10.0,
          "wordCount": 20,
          "status": { "transcribed": true, "summarized": true,
                      "pdfCreated": false, "archived": false, "emailCreated": false },
          "customSections": { "custom.programmierung": "Code-Notizen hier" }
        }
        """.ReplaceLineEndings("\n");

    // Normalised on purpose: the raw literal inherits the source file's line endings,
    // so on a CRLF checkout the newline-based Replace below matched nothing and the
    // test silently asserted against an unmodified document.

    private readonly string tempDir;

    public CustomSectionPersistenceTests()
    {
        this.tempDir = Path.Combine(Path.GetTempPath(), $"JohannCustomSections_{Guid.NewGuid():N}");
        Directory.CreateDirectory(this.tempDir);
    }

    public void Dispose()
    {
        if (Directory.Exists(this.tempDir))
        {
            Directory.Delete(this.tempDir, recursive: true);
        }
    }

    [Fact]
    public void Migrate_V4Document_PreservesCustomSections()
    {
        var dto = JsonMigrator.Migrate(JsonDocument.Parse(V4Json).RootElement);

        dto.SchemaVersion.Should().Be(4);
        dto.CustomSections.Should().ContainKey("custom.programmierung")
            .WhoseValue.Should().Be("Code-Notizen hier");
    }

    [Fact]
    public void Migrate_DoesNotDowngradeNewerSchemaVersion()
    {
        var json = V4Json.Replace("\"schemaVersion\": 4", "\"schemaVersion\": 9");

        var dto = JsonMigrator.Migrate(JsonDocument.Parse(json).RootElement);

        dto.SchemaVersion.Should().Be(
            9, "a file written by a newer client must not be stamped backwards");
    }

    [Fact]
    public void Migrate_LegacyV2Document_IsUpgradedToV4WithEmptyCustomSections()
    {
        var json = V4Json
            .Replace("\"schemaVersion\": 4", "\"schemaVersion\": 2")
            .Replace(
                "\"customSections\": { \"custom.programmierung\": \"Code-Notizen hier\" }",
                "\"longSummary\": \"alt\"");

        var dto = JsonMigrator.Migrate(JsonDocument.Parse(json).RootElement);

        dto.SchemaVersion.Should().Be(4);
        dto.CustomSections.Should().BeEmpty();
        dto.LongSummary.Should().Be("alt");
    }

    [Fact]
    public void Migrate_DocumentWithoutCustomSections_YieldsEmptyMapNotNull()
    {
        var json = V4Json.Replace(
            ",\n  \"customSections\": { \"custom.programmierung\": \"Code-Notizen hier\" }",
            string.Empty);

        var dto = JsonMigrator.Migrate(JsonDocument.Parse(json).RootElement);

        dto.CustomSections.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void UnknownFields_SurviveASerialisationCycle()
    {
        var json = V4Json.Replace(
            "\"customSections\"",
            "\"zukunftsFeld\": \"nicht verlieren\",\n  \"customSections\"");

        var dto = JsonMigrator.Migrate(JsonDocument.Parse(json).RootElement);
        var reserialised = JsonSerializer.Serialize(dto);

        reserialised.Should().Contain("zukunftsFeld").And.Contain("nicht verlieren");
    }

    [Fact]
    public async Task Repository_RoundTrip_PreservesCustomSections()
    {
        var repo = new JsonRepository(this.tempDir);
        var date = new DateOnly(2026, 9, 7);
        var entry = MakeEntry(date) with
        {
            CustomSections = new Dictionary<string, string>
            {
                ["custom.programmierung"] = "PROGRAMMIER-INHALT",
                ["custom.analyse"] = "ANALYSE-INHALT",
            },
        };

        await repo.SaveAsync(entry);
        var loaded = await repo.GetEntriesForDateAsync(date);

        loaded.Should().HaveCount(1);
        loaded[0].CustomSections.Should().HaveCount(2);
        loaded[0].CustomSections["custom.programmierung"].Should().Be("PROGRAMMIER-INHALT");
        loaded[0].SchemaVersion.Should().Be(4);
    }

    [Fact]
    public async Task Repository_RoundTrip_WithoutCustomSections_YieldsEmptyMap()
    {
        var repo = new JsonRepository(this.tempDir);
        var date = new DateOnly(2026, 9, 7);

        await repo.SaveAsync(MakeEntry(date));
        var loaded = await repo.GetEntriesForDateAsync(date);

        loaded[0].CustomSections.Should().NotBeNull().And.BeEmpty();
    }

    [Fact]
    public void Entry_SchemaVersion_DefaultsTo4()
    {
        MakeEntry(new DateOnly(2026, 9, 7)).SchemaVersion.Should().Be(4);
    }

    private static Entry MakeEntry(DateOnly date) => new()
    {
        JobId = $"{date:yyMMdd}_001_abc",
        SequenceNumber = 1,
        CreatedAt = new DateTimeOffset(date.Year, date.Month, date.Day, 12, 0, 0, TimeSpan.FromHours(1)),
        Type = EntryType.Projekt,
        ProjectName = "Johann",
        Title = "Test",
        SourceType = "audio",
        Status = new ProcessingStatus(true, true, false, false, false),
        Transcript = "transkript",
    };
}
