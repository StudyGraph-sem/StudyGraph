namespace StudyGraph.Application.Graph.DTOs.Responses;

public sealed record TopicResponse(
    Guid Id,
    Guid RoadmapId,
    string Title,
    decimal ImportanceWeight,
    string StudyStatus,
    bool IsCompleted,
    string? Description = null,
    decimal? PositionX = null,
    decimal? PositionY = null,
    int? Level = null
);
