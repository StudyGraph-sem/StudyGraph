namespace StudyGraph.Domain.Entities;

public enum StudyTaskStatus { Todo, InProgress, Completed }
public enum StudyTaskPriority { Low, Normal, High }

public sealed class StudyTask
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Title { get; set; } = "";
    public string? Description { get; set; }
    public StudyTaskStatus Status { get; set; }
    public StudyTaskPriority Priority { get; set; }
    public DateTime? DueAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}
