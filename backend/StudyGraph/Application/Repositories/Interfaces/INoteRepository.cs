using System.Linq.Expressions;
using StudyGraph.Domain.Entities;
namespace StudyGraph.Application.Repositories.Interfaces;

public interface INoteRepository
{
    Task<Note?> GetByIdAsync(
            Guid topicId,
            Guid noteId,
            CancellationToken cancellationToken);
            
    Task<IReadOnlyList<Note>> GetByTopicIdAsync(
        Guid topicId,
        CancellationToken cancellationToken);

    void Add(Note note);
    
    void Delete(Note note);
    
    Task SaveChangesAsync(CancellationToken cancellationToken);
}