using Auth.Domain.Tokens;

namespace Auth.Application.Tokens;

public interface IResetTokenRepository
{
    Task AddAsync(ResetToken token, CancellationToken cancellationToken);

    Task<ResetToken?> GetByHashAsync(string tokenHash, CancellationToken cancellationToken);
}
