namespace StudyGraph.Application.Graph.DTOs.Requests;

public sealed record RoadmapInput(
    string Title,
    string? Description = null
);
