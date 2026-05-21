namespace Platee.Johann.Infrastructure.Json;

using Platee.Johann.Domain.Entities;
using Platee.Johann.Domain.Enums;
using Platee.Johann.Domain.ValueObjects;

internal static class EntryMapper
{
    public static Entry ToDomain(EntryDto dto) => new()
    {
        JobId = dto.JobId,
        SequenceNumber = dto.SequenceNumber,
        CreatedAt = dto.CreatedAt,
        Type = ParseType(dto.Type),
        ProjectName = dto.ProjectName,
        Title = dto.Title,
        SourceType = dto.SourceType,
        Status = new ProcessingStatus(
                             dto.Status.Transcribed,
                             dto.Status.Summarized,
                             dto.Status.PdfCreated,
                             dto.Status.Archived,
                             dto.Status.EmailCreated),
        Transcript = dto.Transcript,
        Abstract = dto.Abstract,
        LongSummary = dto.LongSummary,
        ProseSummary = dto.ProseSummary,
        EmailText = dto.EmailText,
        ConversationNote = dto.ConversationNote,
        TaskList = dto.TaskList,
        StundenzettelText = dto.StundenzettelText,
        AnalogText = dto.AnalogText,
        IsDone = dto.IsDone,
        DurationSeconds = dto.DurationSeconds,
        WordCount = dto.WordCount,
        SchemaVersion = dto.SchemaVersion,
    };

    public static EntryDto ToDto(Entry entry) => new()
    {
        SchemaVersion = entry.SchemaVersion,
        JobId = entry.JobId,
        SequenceNumber = entry.SequenceNumber,
        CreatedAt = entry.CreatedAt,
        Type = entry.Type.ToString(),
        ProjectName = entry.ProjectName,
        Title = entry.Title,
        SourceType = entry.SourceType,
        Status = new StatusDto
        {
            Transcribed = entry.Status.Transcribed,
            Summarized = entry.Status.Summarized,
            PdfCreated = entry.Status.PdfCreated,
            Archived = entry.Status.Archived,
            EmailCreated = entry.Status.EmailCreated,
        },
        Transcript = entry.Transcript,
        Abstract = entry.Abstract,
        LongSummary = entry.LongSummary,
        ProseSummary = entry.ProseSummary,
        EmailText = entry.EmailText,
        ConversationNote = entry.ConversationNote,
        TaskList = entry.TaskList,
        StundenzettelText = entry.StundenzettelText,
        AnalogText = entry.AnalogText,
        IsDone = entry.IsDone,
        DurationSeconds = entry.DurationSeconds,
        WordCount = entry.WordCount,
    };

    private static EntryType ParseType(string type) =>
        Enum.TryParse<EntryType>(type, ignoreCase: true, out var result)
            ? result
            : EntryType.Projekt;
}
