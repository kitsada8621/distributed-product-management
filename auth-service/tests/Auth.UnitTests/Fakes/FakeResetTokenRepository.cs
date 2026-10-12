using Auth.Application.Tokens;
using Auth.Domain.Tokens;

namespace Auth.UnitTests.Fakes;

public sealed class FakeResetTokenRepository : IResetTokenRepository
{
    public List<ResetToken> Items { get; } = [];

    public Task AddAsync(ResetToken token, CancellationToken cancellationToken)
    {
        Items.Add(token);
        return Task.CompletedTask;
    }

    public Task<ResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(Items.FirstOrDefault(t => t.TokenHash == tokenHash));
    }
}
