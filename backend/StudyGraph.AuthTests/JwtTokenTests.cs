using System.Security.Claims;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.JsonWebTokens;
using StudyGraph.Application.Services.Implementations;
using StudyGraph.Domain.Entities;
namespace StudyGraph.AuthTests;
public sealed class JwtTokenTests
{
    [Theory]
    [InlineData("User")]
    [InlineData("Admin")]
    public void GeneratesSubjectRoleAndExpiration(string role)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["Jwt:Key"] = new string('a', 64), ["Jwt:Issuer"] = "StudyGraph", ["Jwt:Audience"] = "StudyGraph.Client"
        }).Build();
        var user = new User { Id = Guid.NewGuid(), Email = "test@example.com", Role = role };
        var (raw, expires) = new JwtTokenService(configuration).GenerateAccessToken(user);
        var token = new JsonWebToken(raw);
        Assert.Equal(user.Id.ToString(), token.Subject);
        Assert.Equal(role, token.GetClaim(ClaimTypes.Role).Value);
        Assert.InRange(expires, DateTime.UtcNow.AddMinutes(14), DateTime.UtcNow.AddMinutes(16));
    }
}
