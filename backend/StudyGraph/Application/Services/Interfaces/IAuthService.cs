using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Application.DTOs.Responses;

namespace StudyGraph.Application.Services.Interfaces;

public interface IAuthService
{
    Task<UserResponse?> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default);

    Task<AuthResponse?> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default);
}
