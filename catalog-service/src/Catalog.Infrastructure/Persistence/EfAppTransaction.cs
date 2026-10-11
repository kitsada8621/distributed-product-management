using Catalog.Application.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Catalog.Infrastructure.Persistence;

public sealed class EfAppTransaction(IDbContextTransaction transaction, DbContext context) : IAppTransaction
{
    public Task CommitAsync(CancellationToken cancellationToken)
    {
        return transaction.CommitAsync(cancellationToken);
    }

    public async Task RollbackAsync(CancellationToken cancellationToken)
    {
        await transaction.RollbackAsync(cancellationToken);
        // Entities from the failed attempt must not be saved by a later SaveChanges.
        context.ChangeTracker.Clear();
    }

    // Disposing an EF transaction that was not committed rolls it back.
    public ValueTask DisposeAsync()
    {
        return transaction.DisposeAsync();
    }
}
