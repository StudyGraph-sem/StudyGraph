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

        group.MapPost("/refresh", RefreshAsync).RequireRateLimiting("Auth");
        group.MapPost("/logout", LogoutAsync);
        group.MapGet("/admin", () => Results.Ok()).RequireAuthorization(p => p.RequireRole("Admin"));

        group.MapPost("/register", RegisterAsync)
            .RequireRateLimiting("Auth")
            .WithName("Register")
            .Produces<UserResponse>(201)
            .ProducesValidationProblem()
            .Produces(409);

        group.MapPost("/login", LoginAsync)
            .RequireRateLimiting("Auth")
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
        HttpContext context,
        IRefreshTokenService refreshTokens,
        IConfiguration configuration,
        IAuthService authService,
        CancellationToken cancellationToken)
    {
        if (!IsTrustedRequest(context, configuration)) return Results.StatusCode(403);
        if (string.IsNullOrWhiteSpace(request.Email) || request.Email.Length > 255 ||
            string.IsNullOrWhiteSpace(request.Password) || request.Password.Length > 128)
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

        if (result is null) return Results.Unauthorized();
        var session = await refreshTokens.CreateAsync(result, cancellationToken);
        SetCookie(context, session);
        context.Response.Headers.CacheControl = "no-store";
        return Results.Ok(result);
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

    private static bool IsTrustedRequest(HttpContext context, IConfiguration configuration)
    {
        var origin = context.Request.Headers.Origin.ToString();
        return context.Request.Headers["X-StudyGraph-CSRF"] == "1"
            && (origin == $"{context.Request.Scheme}://{context.Request.Host}"
                || origin == (configuration["Frontend:Origin"] ?? "http://localhost:5173"));
    }
    private static CookieOptions Cookie(DateTime? expires = null) => new()
    {
        HttpOnly = true, Secure = true, SameSite = SameSiteMode.Strict, Path = "/", Expires = expires
    };
    private static void SetCookie(HttpContext context, RefreshSession session) =>
        context.Response.Cookies.Append("__Host-StudyGraph.Refresh", session.Token, Cookie(session.ExpiresAt));
    private static async Task<IResult> RefreshAsync(HttpContext context, IConfiguration configuration, IRefreshTokenService service, CancellationToken cancellationToken)
    {
        context.Response.Headers.CacheControl = "no-store";
        if (!IsTrustedRequest(context, configuration)) return Results.StatusCode(403);
        var raw = context.Request.Cookies["__Host-StudyGraph.Refresh"];
        if (raw is null) return Results.Unauthorized();
        var result = await service.RefreshAsync(raw, cancellationToken);
        if (result is null)
        {
            context.Response.Cookies.Delete("__Host-StudyGraph.Refresh", Cookie());
            return Results.Unauthorized();
        }
        SetCookie(context, result);
        return Results.Ok(result.Auth);
    }
    private static async Task<IResult> LogoutAsync(HttpContext context, IConfiguration configuration, IRefreshTokenService service, CancellationToken cancellationToken)
    {
        if (!IsTrustedRequest(context, configuration)) return Results.StatusCode(403);
        var raw = context.Request.Cookies["__Host-StudyGraph.Refresh"];
        if (raw is not null) await service.LogoutAsync(raw, cancellationToken);
        context.Response.Cookies.Delete("__Host-StudyGraph.Refresh", Cookie());
        return Results.NoContent();
    }
}
