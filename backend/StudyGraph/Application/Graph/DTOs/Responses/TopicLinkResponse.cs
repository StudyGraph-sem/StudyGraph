namespace StudyGraph.Application.Graph.DTOs.Responses;

public sealed record TopicLinkResponse(Guid RoadmapId, Guid ParentTopicId, Guid ChildTopicId);
