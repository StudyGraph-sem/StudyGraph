namespace StudyGraph.Domain.Entities.Graph;

public class Topic
{
    public Guid Id { get; set; }
    public Guid RoadmapId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public StudyStatus StudyStatus { get; set; }
    public required decimal ImportanceWeight { get; set; }
    public decimal? PositionX { get; set; }
    public decimal? PositionY { get; set; }
}
