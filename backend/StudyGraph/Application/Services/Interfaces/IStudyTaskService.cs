using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Application.DTOs.Responses;
namespace StudyGraph.Application.Services.Interfaces;

public interface IStudyTaskService
{
    Task<StudyTaskResponse> CreateAsync(Guid userId, CreateStudyTaskRequest request, CancellationToken cancellationToken);
    Task<StudyTaskPageResponse> ListAsync(Guid userId, StudyTaskListRequest request, CancellationToken cancellationToken);
    Task<StudyTaskResponse?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<StudyTaskResponse?> UpdateAsync(Guid userId, Guid id, UpdateStudyTaskRequest request, CancellationToken cancellationToken);
    Task<StudyTaskResponse?> ChangeStatusAsync(Guid userId, Guid id, ChangeStudyTaskStatusRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
