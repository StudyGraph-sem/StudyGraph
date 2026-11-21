using StudyGraph.Domain.Entities;
namespace StudyGraph.Application.Repositories.Interfaces;
public interface IRefreshTokenRepository
{
    Task AddAsync(RefreshToken token, CancellationToken cancellationToken);
    Task<RefreshToken?> RotateAsync(string hash, RefreshToken replacement, CancellationToken cancellationToken);
    Task RevokeAsync(string hash, CancellationToken cancellationToken);
}
