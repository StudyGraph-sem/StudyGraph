namespace StudyGraph.Domain.Entities.Graph;

public class Roadmap
{
    public Guid Id { get; set; }
    public Guid SubjectId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
}
