namespace StudyGraph.Application.DTOs.Responses;

public sealed record RoadmapProgressResponse(
    Guid UserId,
    Guid RoadmapId,
    decimal Percentage,
    decimal TotalWeight,
    decimal CompletedWeight,
    int TotalTopicCount,
    int CompletedTopicCount,
    IReadOnlyList<Guid> CompletedTopicIds,
    IReadOnlyList<Guid> IncompleteTopicIds);
