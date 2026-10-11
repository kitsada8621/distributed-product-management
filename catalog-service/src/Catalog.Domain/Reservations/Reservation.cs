namespace Catalog.Domain.Reservations;

public class Reservation
{
    public Guid OrderId { get; private set; }
    public ReservationStatus Status { get; private set; }
    public List<ReservationItem> Items { get; private set; } = [];
    public DateTime CreatedAt { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    private Reservation()
    {
    }

    public static Reservation Reserve(Guid orderId, IEnumerable<(Guid ProductId, int Quantity)> items, DateTime now)
    {
        var reservation = CreateWithStatus(orderId, ReservationStatus.Reserved, now);
        foreach (var (productId, quantity) in items)
        {
            reservation.Items.Add(new ReservationItem(orderId, productId, quantity));
        }
        return reservation;
    }

    // Created when order.cancelled arrives before order.created, so the late order.created deducts nothing.
    public static Reservation CreateTombstone(Guid orderId, DateTime now)
    {
        return CreateWithStatus(orderId, ReservationStatus.Released, now);
    }

    public bool Commit(DateTime now)
    {
        return ChangeFromReserved(ReservationStatus.Committed, now);
    }

    public bool Release(DateTime now)
    {
        return ChangeFromReserved(ReservationStatus.Released, now);
    }

    private bool ChangeFromReserved(ReservationStatus next, DateTime now)
    {
        if (Status != ReservationStatus.Reserved)
        {
            return false;
        }

        Status = next;
        UpdatedAt = now;
        return true;
    }

    private static Reservation CreateWithStatus(Guid orderId, ReservationStatus status, DateTime now)
    {
        return new Reservation
        {
            OrderId = orderId,
            Status = status,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
