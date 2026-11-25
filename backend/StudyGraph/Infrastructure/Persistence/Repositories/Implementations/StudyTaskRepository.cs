using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using StudyGraph.Application.Repositories.Interfaces;
using StudyGraph.Domain.Entities;
namespace StudyGraph.Infrastructure.Persistence.Repositories.Implementations;

public sealed class StudyTaskRepository(AppDbContext context) : IStudyTaskRepository
{
    public Task<StudyTask?> GetByIdAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        context.StudyTasks.SingleOrDefaultAsync(x => x.UserId == userId && x.Id == id, cancellationToken);

    public async Task<(IReadOnlyList<StudyTask> Items, int TotalCount)> ListAsync(
        Guid userId, Expression<Func<StudyTask, bool>> filter, bool dueDescending,
        int page, int pageSize, CancellationToken cancellationToken)
    {
        var query = context.StudyTasks.AsNoTracking().Where(x => x.UserId == userId).Where(filter);
        var total = await query.CountAsync(cancellationToken);
        var ordered = query.OrderBy(x => x.DueAt == null);
        var byDeadline = dueDescending ? ordered.ThenByDescending(x => x.DueAt) : ordered.ThenBy(x => x.DueAt);
        var items = await byDeadline.ThenBy(x => x.CreatedAt).ThenBy(x => x.Id)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(cancellationToken);
        return (items, total);
    }
    public async Task AddAsync(StudyTask task, CancellationToken cancellationToken)
    {
        context.StudyTasks.Add(task);
        await context.SaveChangesAsync(cancellationToken);
    }
    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
    public async Task<bool> DeleteAsync(Guid userId, Guid id, CancellationToken cancellationToken) =>
        await context.StudyTasks.Where(x => x.UserId == userId && x.Id == id).ExecuteDeleteAsync(cancellationToken) > 0;
}
