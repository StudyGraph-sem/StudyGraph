namespace StudyGraph.Application.Graph.DTOs.Responses;

public sealed record SubjectResponse(
    Guid Id,
    string Title,
    string Visibility,
    string? Description = null
);
