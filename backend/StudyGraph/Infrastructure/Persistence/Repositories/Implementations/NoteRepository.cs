using Microsoft.EntityFrameworkCore;
using StudyGraph.Application.Repositories.Interfaces;
using StudyGraph.Domain.Entities;
namespace StudyGraph.Infrastructure.Persistence.Repositories.Implementations;

public sealed class NoteRepository(AppDbContext context) : INoteRepository
{
    public Task<Note?> GetByIdAsync(Guid topicId, Guid noteId, CancellationToken cancellationToken) =>
        context.Notes.SingleOrDefaultAsync(x => x.TopicId == topicId && x.Id == noteId, cancellationToken);

    public async Task<IReadOnlyList<Note>> GetByTopicIdAsync(
        Guid topicId,
        CancellationToken cancellationToken)
    {
        return await context.Notes
            .AsNoTracking()
            .Where(x => x.TopicId == topicId)
            .OrderByDescending(x => x.CreatedAt)
            .ThenBy(x => x.Id)
            .ToListAsync(cancellationToken);
    }
    
    public void Add(Note note) => context.Notes.Add(note);  
    
    public void Delete(Note note) => context.Notes.Remove(note); 
    
    public Task SaveChangesAsync(CancellationToken cancellationToken) => context.SaveChangesAsync(cancellationToken);
}
