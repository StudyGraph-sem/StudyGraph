using Microsoft.EntityFrameworkCore;
using StudyGraph.Domain.Entities;
using StudyGraph.Application.Repositories.Interfaces;
namespace StudyGraph.Infrastructure.Persistence.Repositories.Implementations;
public sealed class RefreshTokenRepository(AppDbContext context) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        context.Set<RefreshToken>().Add(token);
        await context.SaveChangesAsync(cancellationToken);
    }
    public async Task<RefreshToken?> RotateAsync(string hash, RefreshToken replacement, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var token = await context.Set<RefreshToken>().AsNoTracking().SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (token is null) return null;
        await context.Users.Where(x => x.Id == token.UserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Role, x => x.Role), cancellationToken);
        token = await context.Set<RefreshToken>().AsNoTracking().SingleAsync(x => x.Id == token.Id, cancellationToken);
        var now = DateTime.UtcNow;
        if (token.RevokedAt is not null)
        {
            await RevokeFamilyAsync(token.FamilyId, now, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return null;
        }
        if (token.ExpiresAt <= now) return null;
        await context.Set<RefreshToken>().Where(x => x.Id == token.Id).ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now), cancellationToken);
        replacement.UserId = token.UserId;
        replacement.FamilyId = token.FamilyId;
        replacement.ExpiresAt = token.ExpiresAt;
        context.Set<RefreshToken>().Add(replacement);
        await context.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return replacement;
    }
    public async Task RevokeAsync(string hash, CancellationToken cancellationToken)
    {
        await using var transaction = await context.Database.BeginTransactionAsync(cancellationToken);
        var token = await context.Set<RefreshToken>().AsNoTracking().SingleOrDefaultAsync(x => x.TokenHash == hash, cancellationToken);
        if (token is null) return;
        await context.Users.Where(x => x.Id == token.UserId).ExecuteUpdateAsync(s => s.SetProperty(x => x.Role, x => x.Role), cancellationToken);
        await RevokeFamilyAsync(token.FamilyId, DateTime.UtcNow, cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }
    private Task<int> RevokeFamilyAsync(Guid familyId, DateTime now, CancellationToken cancellationToken) =>
        context.Set<RefreshToken>().Where(x => x.FamilyId == familyId && x.RevokedAt == null)
            .ExecuteUpdateAsync(s => s.SetProperty(x => x.RevokedAt, now), cancellationToken);
}
