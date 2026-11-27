using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Application.Services.Interfaces;

namespace StudyGraph.API.Endpoints;
//TODO: добавить проверку доступа к топику
public static class NoteEndpoints
{
    public static IEndpointRouteBuilder MapNoteEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/topics/{topicId:guid}/notes")
            .WithTags("Notes");

        group.MapGet("", async (Guid topicId, INoteService service, CancellationToken ct) =>
        Results.Ok(await service.GetByTopicIdAsync(topicId, ct)))
            .WithName("GetNotesByTopic");
        
        group.MapGet("/{noteId:guid}", async (Guid topicId, Guid noteId, INoteService service, CancellationToken ct) =>
        {
            var note = await service.GetByIdAsync(topicId, noteId, ct);
            return note is null ? Results.NotFound() : Results.Ok(note);
        }).WithName("GetNoteById");
        
        group.MapPost("", async (Guid topicId, CreateNoteRequest request, INoteService service, CancellationToken ct) =>
        {
            var note = await service.CreateAsync(topicId, request, ct);
            return Results.CreatedAtRoute("GetNoteById", new { topicId = note.TopicId, noteId = note.Id }, note);
        }).WithName("CreateNote");

        group.MapPut("/{noteId:guid}",
            async (Guid topicId, Guid noteId, UpdateNoteRequest request, INoteService service, CancellationToken ct) =>
            {
                var note = await service.UpdateAsync(topicId, noteId, request, ct);
                return note is null ? Results.NotFound() : Results.Ok(note);
            }).WithName("UpdateNote");

        group.MapDelete("/{noteId:guid}", async (Guid topicId, Guid noteId, INoteService service, CancellationToken ct) =>
            await service.DeleteAsync(topicId, noteId, ct) ? Results.NoContent() : Results.NotFound())
            .WithName("DeleteNote");
        return app;
    }
}