namespace StudyGraph.Application.Graph.DTOs.Requests;

public sealed record TopicInput(
    string Title,
    decimal ImportanceWeight,
    string? Description = null,
    decimal? PositionX = null,
    decimal? PositionY = null,
    int? Level = null
);
