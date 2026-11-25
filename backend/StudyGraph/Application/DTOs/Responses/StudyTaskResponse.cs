using StudyGraph.Domain.Entities;
namespace StudyGraph.Application.DTOs.Responses;

public sealed record StudyTaskResponse(
    Guid Id, Guid UserId, string Title, string? Description,
    StudyTaskStatus Status, StudyTaskPriority Priority,
    DateTime? DueAt, DateTime CreatedAt, DateTime UpdatedAt,
    DateTime? CompletedAt, bool IsOverdue);

public sealed record StudyTaskPageResponse(
    IReadOnlyList<StudyTaskResponse> Items, int TotalCount, int Page, int PageSize);
