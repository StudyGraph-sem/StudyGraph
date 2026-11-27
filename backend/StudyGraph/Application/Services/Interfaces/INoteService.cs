using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Application.DTOs.Responses;
using StudyGraph.Domain.Entities;

namespace StudyGraph.Application.Services.Interfaces;

public interface INoteService
{
    Task<NoteResponse?> GetByIdAsync(Guid topicId, Guid id, CancellationToken cancellationToken);
    Task<IReadOnlyList<NoteResponse>> GetByTopicIdAsync(Guid topicId, CancellationToken cancellationToken);
    Task<NoteResponse> CreateAsync(Guid topicId, CreateNoteRequest request, CancellationToken cancellationToken);
    Task<NoteResponse?> UpdateAsync(Guid topicId, Guid id, UpdateNoteRequest request, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(Guid topicId, Guid id, CancellationToken cancellationToken);
}
