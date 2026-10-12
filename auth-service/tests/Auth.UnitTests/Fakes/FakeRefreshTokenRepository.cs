using Auth.Application.Tokens;
using Auth.Domain.Tokens;

namespace Auth.UnitTests.Fakes;

public sealed class FakeRefreshTokenRepository : IRefreshTokenRepository
{
    public List<RefreshToken> Items { get; } = [];

    public Task AddAsync(RefreshToken token, CancellationToken cancellationToken)
    {
        Items.Add(token);
        return Task.CompletedTask;
    }

    public Task<RefreshToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken)
    {
        return Task.FromResult(Items.FirstOrDefault(t => t.TokenHash == tokenHash));
    }

    public Task<IReadOnlyList<RefreshToken>> GetFamilyAsync(Guid familyId, CancellationToken cancellationToken)
    {
        IReadOnlyList<RefreshToken> family = Items.Where(t => t.FamilyId == familyId).ToList();
        return Task.FromResult(family);
    }

    public Task<IReadOnlyList<RefreshToken>> GetByUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        IReadOnlyList<RefreshToken> tokens = Items.Where(t => t.UserId == userId).ToList();
        return Task.FromResult(tokens);
    }
}
