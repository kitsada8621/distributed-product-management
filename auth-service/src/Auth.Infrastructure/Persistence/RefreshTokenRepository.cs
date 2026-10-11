using Auth.Application.Tokens;
using Auth.Domain.Tokens;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

public sealed class RefreshTokenRepository(AuthDbContext context) : IRefreshTokenRepository
{
    public async Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        await context.RefreshTokens.AddAsync(token, cancellationToken);
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return context.RefreshTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        return await context.RefreshTokens.Where(t => t.FamilyId == familyId).ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<RefreshToken>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        return await context.RefreshTokens.Where(t => t.UserId == userId).ToListAsync(cancellationToken);
    }
}
