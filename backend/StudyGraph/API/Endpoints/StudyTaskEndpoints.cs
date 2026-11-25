using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Application.Services.Interfaces;
namespace StudyGraph.API.Endpoints;

public static class StudyTaskEndpoints
{
    public static IEndpointRouteBuilder MapStudyTaskEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/tasks").WithTags("Study tasks").RequireAuthorization();
        group.AddEndpointFilter(async (context, next) =>
        {
            if (!Guid.TryParse(context.HttpContext.User.FindFirst("sub")?.Value, out var id) || id == Guid.Empty)
                return Results.Unauthorized();
            try { return await next(context); }
            catch (ValidationException error)
            {
                var key = error.ValidationResult?.MemberNames.FirstOrDefault() ?? "request";
                return Results.ValidationProblem(new Dictionary<string, string[]> { [key] = [error.Message] });
            }
        });
        group.MapPost("/", async (ClaimsPrincipal user, CreateStudyTaskRequest request, IStudyTaskService service, CancellationToken ct) =>
        {
            var task = await service.CreateAsync(UserId(user), request, ct);
            return Results.Created($"/api/tasks/{task.Id}", task);
        });
        group.MapGet("/", async (ClaimsPrincipal user, [AsParameters] StudyTaskListRequest request, IStudyTaskService service, CancellationToken ct) =>
            Results.Ok(await service.ListAsync(UserId(user), request, ct)));
        group.MapGet("/{id:guid}", async (ClaimsPrincipal user, Guid id, IStudyTaskService service, CancellationToken ct) =>
        {
            var task = await service.GetByIdAsync(UserId(user), id, ct);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });
        group.MapPut("/{id:guid}", async (ClaimsPrincipal user, Guid id, UpdateStudyTaskRequest request, IStudyTaskService service, CancellationToken ct) =>
        {
            var task = await service.UpdateAsync(UserId(user), id, request, ct);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });
        group.MapPatch("/{id:guid}/status", async (ClaimsPrincipal user, Guid id, ChangeStudyTaskStatusRequest request, IStudyTaskService service, CancellationToken ct) =>
        {
            var task = await service.ChangeStatusAsync(UserId(user), id, request, ct);
            return task is null ? Results.NotFound() : Results.Ok(task);
        });
        group.MapDelete("/{id:guid}", async (ClaimsPrincipal user, Guid id, IStudyTaskService service, CancellationToken ct) =>
            await service.DeleteAsync(UserId(user), id, ct) ? Results.NoContent() : Results.NotFound());
        return app;
    }
    private static Guid UserId(ClaimsPrincipal principal) => Guid.Parse(principal.FindFirst("sub")!.Value);
}
