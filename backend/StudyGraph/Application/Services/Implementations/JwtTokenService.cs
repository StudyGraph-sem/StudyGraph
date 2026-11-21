using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using StudyGraph.Application.Services.Interfaces;
using StudyGraph.Domain.Entities;

namespace StudyGraph.Application.Services.Implementations;

public sealed class JwtTokenService(
    IConfiguration configuration) : IJwtTokenService
{
    public (string Token, DateTime ExpiresAt)
        GenerateAccessToken(User user)
    {
        var key = configuration["Jwt:Key"]
                  ?? throw new InvalidOperationException(
                      "JWT key is not configured.");

        var issuer = configuration["Jwt:Issuer"]
                     ?? throw new InvalidOperationException(
                         "JWT issuer is not configured.");

        var audience = configuration["Jwt:Audience"]
                       ?? throw new InvalidOperationException(
                           "JWT audience is not configured.");

        if (user.Role is not ("User" or "Admin"))
            throw new InvalidOperationException("Unsupported user role.");
        var expiresAt = DateTime.UtcNow.AddMinutes(15);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub,
                user.Id.ToString()),

            new Claim(JwtRegisteredClaimNames.Email,
                user.Email),

            new Claim(ClaimTypes.Role, user.Role),

            new Claim(JwtRegisteredClaimNames.Jti,
                Guid.NewGuid().ToString())
        };

        var descriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = expiresAt,
            Issuer = issuer,
            Audience = audience,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(key)),
                SecurityAlgorithms.HmacSha256)
        };

        var handler = new JsonWebTokenHandler();
        var token = handler.CreateToken(descriptor);

        return (token, expiresAt);
    }
}