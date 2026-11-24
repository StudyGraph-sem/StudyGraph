namespace StudyGraph.Application.Graph.DTOs.Responses;

public sealed record RoadmapResponse(
    Guid Id,
    Guid SubjectId,
    string Title,
    string? Description = null
);
