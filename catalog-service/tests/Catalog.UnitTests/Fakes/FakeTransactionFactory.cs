using Catalog.Application.Common;

namespace Catalog.UnitTests.Fakes;

public sealed class FakeTransactionFactory(FakeStockRepository stock) : ITransactionFactory
{
    public int Begun { get; private set; }

    public int Committed { get; private set; }

    public int RolledBack { get; private set; }

    public Task<IAppTransaction> BeginAsync(CancellationToken cancellationToken)
    {
        Begun++;
        return Task.FromResult<IAppTransaction>(new FakeTransaction(this, stock.TakeCopy(), stock));
    }

    private sealed class FakeTransaction(FakeTransactionFactory owner, object copy, FakeStockRepository stock)
        : IAppTransaction
    {
        private bool _finished;

        public Task CommitAsync(CancellationToken cancellationToken)
        {
            _finished = true;
            owner.Committed++;
            return Task.CompletedTask;
        }

        public Task RollbackAsync(CancellationToken cancellationToken)
        {
            _finished = true;
            owner.RolledBack++;
            stock.RestoreCopy(copy);
            return Task.CompletedTask;
        }

        public ValueTask DisposeAsync()
        {
            if (!_finished)
            {
                owner.RolledBack++;
                stock.RestoreCopy(copy);
            }
            return ValueTask.CompletedTask;
        }
    }
}
