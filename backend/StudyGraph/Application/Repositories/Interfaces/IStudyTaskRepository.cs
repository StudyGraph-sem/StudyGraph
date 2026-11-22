using System.Linq.Expressions;
using StudyGraph.Domain.Entities;
namespace StudyGraph.Application.Repositories.Interfaces;

public interface IStudyTaskRepository
{
    Task<StudyTask?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken);
    Task<(IReadOnlyList<StudyTask> Items, int TotalCount)> ListAsync(
        Guid userId, Expression<Func<StudyTask, bool>> filter, bool dueDescending,
        int page, int pageSize, CancellationToken cancellationToken);
    Task AddAsync(StudyTask task, CancellationToken cancellationToken);
    Task SaveChangesAsync(CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken);
}
