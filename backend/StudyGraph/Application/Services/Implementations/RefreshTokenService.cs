using System.Security.Cryptography;
using System.Text;
using StudyGraph.Application.DTOs.Responses;
using StudyGraph.Application.Repositories.Interfaces;
using StudyGraph.Application.Services.Interfaces;
using StudyGraph.Domain.Entities;
namespace StudyGraph.Application.Services.Implementations;
public sealed class RefreshTokenService(IRefreshTokenRepository repository, IUserRepository users, IJwtTokenService jwt) : IRefreshTokenService
{
    public async Task<RefreshSession> CreateAsync(AuthResponse auth, CancellationToken cancellationToken)
    {
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var token = NewToken(raw);
        token.UserId = auth.User.Id;
        token.FamilyId = Guid.NewGuid();
        await repository.AddAsync(token, cancellationToken);
        return new(raw, token.ExpiresAt, auth);
    }
    public async Task<RefreshSession?> RefreshAsync(string token, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(token) || token.Length > 256) return null;
        var raw = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));
        var replacement = await repository.RotateAsync(Hash(token), NewToken(raw), cancellationToken);
        if (replacement is null) return null;
        var user = await users.GetByIdAsync(replacement.UserId, cancellationToken);
        if (user is null) return null;
        var (access, expires) = jwt.GenerateAccessToken(user);
        return new(raw, replacement.ExpiresAt, new(access, expires, new(user.Id, user.Email, user.DisplayName, user.Role, user.CreatedAt)));
    }
    public Task LogoutAsync(string token, CancellationToken cancellationToken) => repository.RevokeAsync(Hash(token), cancellationToken);
    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
    private static RefreshToken NewToken(string raw) => new() { Id = Guid.NewGuid(), TokenHash = Hash(raw), ExpiresAt = DateTime.UtcNow.AddDays(7) };
}
