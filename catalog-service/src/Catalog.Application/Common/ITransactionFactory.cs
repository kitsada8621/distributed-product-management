namespace Catalog.Application.Common;

public interface ITransactionFactory
{
    Task<IAppTransaction> BeginAsync(CancellationToken cancellationToken);
}

// Disposing without CommitAsync rolls back.
public interface IAppTransaction : IAsyncDisposable
{
    Task CommitAsync(CancellationToken cancellationToken);

    Task RollbackAsync(CancellationToken cancellationToken);
}
