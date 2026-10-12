using Auth.Application.Users;
using Auth.Domain.Users;

namespace Auth.UnitTests.Fakes;

public sealed class FakeUserRepository : IUserRepository
{
    public List<User> Items { get; } = [];

    public Task<User?> GetByEmailAsync(string email, CancellationToken cancellationToken)
    {
        var found = Items.FirstOrDefault(u => string.Equals(u.Email, email, StringComparison.OrdinalIgnoreCase));
        return Task.FromResult(found);
    }

    public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken)
    {
        return Task.FromResult(Items.FirstOrDefault(u => u.Id == id));
    }

    public Task AddAsync(User user, CancellationToken cancellationToken)
    {
        Items.Add(user);
        return Task.CompletedTask;
    }
}
