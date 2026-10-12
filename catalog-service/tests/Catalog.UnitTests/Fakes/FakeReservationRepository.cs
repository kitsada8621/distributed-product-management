using Catalog.Application.Reservations;
using Catalog.Domain.Reservations;

namespace Catalog.UnitTests.Fakes;

public sealed class FakeReservationRepository : IReservationRepository
{
    public List<Reservation> Items { get; } = [];

    public Task<Reservation?> FindAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return Task.FromResult(Items.FirstOrDefault(reservation => reservation.OrderId == orderId));
    }

    public Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        Items.Add(reservation);
        return Task.CompletedTask;
    }
}
