namespace StudyGraph.Domain.Entities;

public class Note
{
    public Guid Id { get; set; }
    public Guid TopicId { get; set; }
    public required string Title { get; set; } 
    public required string Content { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}