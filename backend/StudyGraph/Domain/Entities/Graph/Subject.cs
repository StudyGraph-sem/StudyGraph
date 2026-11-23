namespace StudyGraph.Domain.Entities.Graph;

public class Subject
{
    public Guid Id { get; set; }
    public Guid OwnerUserId { get; set; }
    public required string Title { get; set; }
    public string? Description { get; set; }
    public SubjectVisibility Visibility { get; set; }
    public Guid? ShareToken { get; set; }
}
