namespace StudyGraph.Application.DTOs.Responses;

public sealed record NoteResponse(
    Guid Id,
    Guid TopicId,
    string Title,
    string Content,
    DateTime CreatedAt,
    DateTime UpdatedAt
);