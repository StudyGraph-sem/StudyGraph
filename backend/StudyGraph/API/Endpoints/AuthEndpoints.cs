using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Application.DTOs.Responses;
using StudyGraph.Application.Repositories.Interfaces;
using StudyGraph.Application.Services.Interfaces;

namespace StudyGraph.API.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(
        this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Auth");

        group.MapPost("/register", RegisterAsync)
            .WithName("Register")
            .Produces<UserResponse>(201)
            .ProducesValidationProblem()
            .Produces(409);

        group.MapPost("/login", LoginAsync)
            .WithName("Login")
            .Produces<AuthResponse>(200)
            .ProducesValidationProblem()
            .Produces(401);

        group.MapGet("/me", GetMeAsync)
            .RequireAuthorization()
            .WithName("GetCurrentUser")
            .Produces<UserResponse>(200)
            .Produces(401)
            .Produces(404);

        return app;
    }

    private static async Task<IResult> RegisterAsync(
        RegisterRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        var errors = new Dictionary<string, string[]>();

        if (string.IsNullOrWhiteSpace(request.Email) ||
            request.Email.Length > 255 ||
            !new EmailAddressAttribute().IsValid(request.Email))
        {
            errors["email"] = ["Invalid email address."];
        }

        if (string.IsNullOrWhiteSpace(request.Password) ||
            request.Password.Length < 8 ||
            request.Password.Length > 128)
        {
            errors["password"] =
                ["Password must contain 8 to 128 characters."];
        }

        if (string.IsNullOrWhiteSpace(request.DisplayName) ||
            request.DisplayName.Length > 100)
        {
            errors["displayName"] =
                ["Display name must contain 1 to 100 characters."];
        }

        if (errors.Count > 0)
            return Results.ValidationProblem(errors);

        var user = await authService.RegisterAsync(
            request, cancellationToken);

        return user is null
            ? Results.Conflict(new
            {
                Message = "Email is already registered."
            })
            : Results.Json(user, statusCode: 201);
    }

    private static async Task<IResult> LoginAsync(
        LoginRequest request,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.Email) ||
            string.IsNullOrWhiteSpace(request.Password))
        {
            return Results.ValidationProblem(
                new Dictionary<string, string[]>
                {
                    ["credentials"] =
                        ["Email and password are required."]
                });
        }

        var result = await authService.LoginAsync(
            request, cancellationToken);

        return result is null
            ? Results.Unauthorized()
            : Results.Ok(result);
    }

    private static async Task<IResult> GetMeAsync(
        ClaimsPrincipal principal,
        IUserRepository repository,
        CancellationToken cancellationToken)
    {
        var userIdString = principal.FindFirst("sub")?.Value;

        if (!Guid.TryParse(userIdString, out var userId))
            return Results.Unauthorized();

        var user = await repository.GetByIdAsync(
            userId, cancellationToken);

        if (user is null)
            return Results.NotFound();

        return Results.Ok(new UserResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            user.CreatedAt
        ));
    }
}
