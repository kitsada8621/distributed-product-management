using Auth.Application.Tokens;
using Auth.Domain.Tokens;
using Microsoft.EntityFrameworkCore;

namespace Auth.Infrastructure.Persistence;

public sealed class ResetTokenRepository(AuthDbContext context) : IResetTokenRepository
{
    public async Task AddAsync(ResetToken token, CancellationToken cancellationToken)
    {
        await context.ResetTokens.AddAsync(token, cancellationToken);
    }

    public Task<ResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return context.ResetTokens.FirstOrDefaultAsync(t => t.TokenHash == tokenHash, cancellationToken);
    }
}
