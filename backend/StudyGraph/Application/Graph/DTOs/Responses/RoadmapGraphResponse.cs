namespace StudyGraph.Application.Graph.DTOs.Responses;

public sealed record RoadmapGraphResponse(
    Guid RoadmapId,
    IReadOnlyList<TopicResponse> Topics,
    IReadOnlyList<TopicLinkResponse> Links,
    bool IsCompleted
);
