using Catalog.Application.Reservations;
using Catalog.Domain.Reservations;
using Microsoft.EntityFrameworkCore;

namespace Catalog.Infrastructure.Persistence;

public sealed class ReservationRepository(CatalogDbContext context) : IReservationRepository
{
    public Task<Reservation?> FindAsync(Guid orderId, CancellationToken cancellationToken)
    {
        return context.Reservations
            .Include(r => r.Items)
            .FirstOrDefaultAsync(r => r.OrderId == orderId, cancellationToken);
    }

    public async Task AddAsync(Reservation reservation, CancellationToken cancellationToken)
    {
        await context.Reservations.AddAsync(reservation, cancellationToken);
    }
}
