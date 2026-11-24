namespace StudyGraph.Application.Graph.DTOs.Requests;

public sealed record SubjectInput(
    string Title,
    string Visibility,
    string? Description = null
);
