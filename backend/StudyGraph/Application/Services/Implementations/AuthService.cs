using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Npgsql;
using StudyGraph.Application.DTOs.Requests;
using StudyGraph.Application.DTOs.Responses;
using StudyGraph.Application.Repositories.Interfaces;
using StudyGraph.Application.Services.Interfaces;
using StudyGraph.Domain.Entities;

namespace StudyGraph.Application.Services.Implementations;

public class AuthService(
    IUserRepository userRepository,
    IPasswordHasher<User> passwordHasher,
    IJwtTokenService jwtTokenService
) : IAuthService
{
    private static readonly User DummyUser = new();
    private static readonly string DummyPasswordHash = new PasswordHasher<User>().HashPassword(DummyUser, Guid.NewGuid().ToString());
    public async Task<UserResponse?> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var email = request.Email.Trim().ToLowerInvariant();
        var displayName = request.DisplayName.Trim();

        if (await userRepository.ExistsByEmailAsync(
                email, cancellationToken))
        {
            return null;
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            DisplayName = displayName,
            Role = "User",
            CreatedAt = DateTime.UtcNow
        };

        user.PasswordHash = passwordHasher.HashPassword(
            user, request.Password);

        await userRepository.AddAsync(user, cancellationToken);

        try
        {
            await userRepository.SaveChangesAsync(
                cancellationToken);
        }
        catch (DbUpdateException ex)
            when (ex.InnerException is PostgresException
                  { SqlState: PostgresErrorCodes.UniqueViolation })
        {
            if (await userRepository.ExistsByEmailAsync(
                    email, cancellationToken))
            {
                return null;
            }

            throw;
        }

        return new UserResponse(
            user.Id,
            user.Email,
            user.DisplayName,
            user.Role,
            user.CreatedAt
        );
    }

public async Task<AuthResponse?> LoginAsync(
    LoginRequest request,
    CancellationToken cancellationToken = default)
{
    var email = request.Email.Trim().ToLowerInvariant();

    var user = await userRepository.GetByEmailAsync(
        email, cancellationToken);

    if (user is null)
    {
        passwordHasher.VerifyHashedPassword(DummyUser, DummyPasswordHash, request.Password);
        return null;
    }

    var verificationResult =
        passwordHasher.VerifyHashedPassword(
            user,
            user.PasswordHash,
            request.Password);

    if (verificationResult ==
        PasswordVerificationResult.Failed)
    {
        return null;
    }

    var (token, expiresAt) =
        jwtTokenService.GenerateAccessToken(user);

    var userResponse = new UserResponse(
        user.Id,
        user.Email,
        user.DisplayName,
        user.Role,
        user.CreatedAt
    );

    return new AuthResponse(
        token,
        expiresAt,
        userResponse
    );
}
}
