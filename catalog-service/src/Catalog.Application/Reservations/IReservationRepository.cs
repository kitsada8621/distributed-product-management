using Catalog.Domain.Reservations;

namespace Catalog.Application.Reservations;

public interface IReservationRepository
{
    // Tracked, with Items loaded.
    Task<Reservation?> FindAsync(Guid orderId, CancellationToken cancellationToken);

    Task AddAsync(Reservation reservation, CancellationToken cancellationToken);
}
