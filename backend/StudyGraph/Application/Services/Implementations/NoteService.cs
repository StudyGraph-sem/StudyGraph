using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Application.DTOs.Responses;
using StudyGraph.Application.Repositories.Interfaces;
using StudyGraph.Application.Services.Interfaces;
using StudyGraph.Domain.Entities;

namespace StudyGraph.Application.Services.Implementations;
//TODO: проверить существование темы перед созданием
public sealed class NoteService(INoteRepository repository) : INoteService
{
    public async Task<NoteResponse?> GetByIdAsync(Guid topicId, Guid id, CancellationToken cancellationToken)
    {
        var note = await repository.GetByIdAsync(topicId, id, cancellationToken);
        return note is null ? null : new NoteResponse(
            note.Id,
            note.TopicId,
            note.Title,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt
        );
    }

    public async Task<IReadOnlyList<NoteResponse>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken)
    {
        var notes = await repository.GetByTopicIdAsync(topicId, cancellationToken);

        return notes.Select(note => new NoteResponse(
            note.Id,
            note.TopicId,
            note.Title,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt
        )).ToList();
    }

    public async Task<NoteResponse> CreateAsync(Guid topicId, CreateNoteRequest request, CancellationToken cancellationToken)
    {
        var now = DateTime.UtcNow;
        
        var note = new Note
        {
            Id = Guid.NewGuid(),
            TopicId = topicId,
            Title = request.Title,
            Content = request.Content,
            CreatedAt = now,
            UpdatedAt = now
        };
        
        repository.Add(note);
        
        await repository.SaveChangesAsync(cancellationToken);
        
        return new NoteResponse(
            note.Id,
            note.TopicId,
            note.Title,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt
        );
    }

    public async Task<NoteResponse?> UpdateAsync(Guid topicId, Guid id, UpdateNoteRequest request, CancellationToken cancellationToken)
    {
        var note = await repository.GetByIdAsync(topicId, id, cancellationToken);
        
        if (note is null)
            return null;

        note.Title = request.Title;
        note.Content = request.Content;
        note.UpdatedAt = DateTime.UtcNow;

        await repository.SaveChangesAsync(cancellationToken);
        return new NoteResponse(
            note.Id,
            note.TopicId,
            note.Title,
            note.Content,
            note.CreatedAt,
            note.UpdatedAt
        );
    }

    public async Task<bool> DeleteAsync(Guid topicId, Guid id, CancellationToken cancellationToken)
    {
        var note = await repository.GetByIdAsync(topicId, id, cancellationToken);

        if (note is null)
            return false;

        repository.Delete(note);
        await repository.SaveChangesAsync(cancellationToken);

        return true;
    }
}