namespace StudyGraph.Application.Graph.DTOs.Requests;

public sealed record TopicLinkInput(Guid ParentTopicId, Guid ChildTopicId);
