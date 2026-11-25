using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Application.DTOs.Responses;
using StudyGraph.Application.Repositories.Interfaces;
using StudyGraph.Application.Services.Interfaces;
using StudyGraph.Application.Validators;
using StudyGraph.Domain.Entities;
namespace StudyGraph.Application.Services.Implementations;

public sealed class StudyTaskService(IStudyTaskRepository repository, StudyTaskValidator validator, TimeProvider clock) : IStudyTaskService
{
    public async Task<StudyTaskResponse> CreateAsync(Guid userId, CreateStudyTaskRequest request, CancellationToken cancellationToken)
    {
        validator.Validate(request);
        var now = clock.GetUtcNow().UtcDateTime;
        var task = new StudyTask {
            Id = Guid.NewGuid(), UserId = userId, Title = request.Title.Trim(),
            Description = NormalizeDescription(request.Description), Priority = request.Priority,
            Status = StudyTaskStatus.Todo, DueAt = request.DueAt?.UtcDateTime, CreatedAt = now, UpdatedAt = now
        };
        await repository.AddAsync(task, cancellationToken);
        return ToResponse(task, now);
    }
    public async Task<StudyTaskPageResponse> ListAsync(Guid userId, StudyTaskListRequest request, CancellationToken cancellationToken)
    {
        validator.Validate(request);
        var now = clock.GetUtcNow().UtcDateTime;
        var result = await repository.ListAsync(userId, task =>
            (request.Status == null || task.Status == request.Status) &&
            (request.Priority == null || task.Priority == request.Priority) &&
            (request.Overdue == null || (task.DueAt != null && task.DueAt < now && task.Status != StudyTaskStatus.Completed) == request.Overdue),
            request.DueDescending, request.Page, request.PageSize, cancellationToken);
        return new(result.Items.Select(task => ToResponse(task, now)).ToArray(), result.TotalCount, request.Page, request.PageSize);
    }
    public async Task<StudyTaskResponse?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken)
    {
        var task = await repository.GetByIdAsync(userId, id, cancellationToken);
        return task is null ? null : ToResponse(task, clock.GetUtcNow().UtcDateTime);
    }
    public async Task<StudyTaskResponse?> UpdateAsync(Guid userId, Guid id, UpdateStudyTaskRequest request, CancellationToken cancellationToken)
    {
        validator.Validate(request);
        var task = await repository.GetByIdAsync(userId, id, cancellationToken);
        if (task is null) return null;
        task.Title = request.Title.Trim();
        task.Description = NormalizeDescription(request.Description);
        task.Priority = request.Priority;
        task.DueAt = request.DueAt?.UtcDateTime;
        task.UpdatedAt = clock.GetUtcNow().UtcDateTime;
        await repository.SaveChangesAsync(cancellationToken);
        return ToResponse(task, task.UpdatedAt);
    }
    public async Task<StudyTaskResponse?> ChangeStatusAsync(Guid userId, Guid id, ChangeStudyTaskStatusRequest request, CancellationToken cancellationToken)
    {
        validator.Validate(request);
        var task = await repository.GetByIdAsync(userId, id, cancellationToken);
        if (task is null) return null;
        var now = clock.GetUtcNow().UtcDateTime;
        if (task.Status != request.Status!.Value)
        {
            task.Status = request.Status.Value;
            task.UpdatedAt = now;
            task.CompletedAt = task.Status == StudyTaskStatus.Completed ? now : null;
            await repository.SaveChangesAsync(cancellationToken);
        }
        return ToResponse(task, now);
    }
    public Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken) => repository.DeleteAsync(userId, id, cancellationToken);
    private static string? NormalizeDescription(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    private static StudyTaskResponse ToResponse(StudyTask task, DateTime now) => new(
        task.Id, task.UserId, task.Title, task.Description, task.Status, task.Priority,
        task.DueAt, task.CreatedAt, task.UpdatedAt, task.CompletedAt,
        task.DueAt.HasValue && task.DueAt.Value < now && task.Status != StudyTaskStatus.Completed);
}
