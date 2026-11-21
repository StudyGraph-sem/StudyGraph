using StudyGraph.Domain.Entities;

namespace StudyGraph.Application.Services.Interfaces;

public interface IJwtTokenService
{
    (string Token, DateTime ExpiresAt)
        GenerateAccessToken(User user);
}
