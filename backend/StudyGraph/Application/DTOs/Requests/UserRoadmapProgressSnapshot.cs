using StudyGraph.Domain.Entities.Graph;
namespace StudyGraph.Application.DTOs.Requests;

public sealed record UserRoadmapProgressSnapshot(
    Guid UserId,
    Guid RoadmapId,
    IReadOnlyCollection<Topic> Topics,
    IReadOnlyCollection<Guid> CompletedTopicIds);
