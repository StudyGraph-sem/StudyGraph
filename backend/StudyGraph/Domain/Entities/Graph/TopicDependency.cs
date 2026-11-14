namespace StudyGraph.Domain.Entities.Graph;

public class TopicDependency
{
    public Guid RoadmapId { get; set; }
    public Guid TopicId { get; set; }
    public Guid PrerequisiteTopicId { get; set; }
}
