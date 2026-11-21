using StudyGraph.Application.DTOs.Responses;
namespace StudyGraph.Application.Services.Interfaces;
public interface IRefreshTokenService
{
    Task<RefreshSession> CreateAsync(AuthResponse auth, CancellationToken cancellationToken);
    Task<RefreshSession?> RefreshAsync(string token, CancellationToken cancellationToken);
    Task LogoutAsync(string token, CancellationToken cancellationToken);
}
