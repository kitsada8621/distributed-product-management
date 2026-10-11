namespace Catalog.Domain.Reservations;

public class ReservationItem
{
    public Guid ReservationId { get; private set; }
    public Guid ProductId { get; private set; }
    public int Quantity { get; private set; }

    private ReservationItem()
    {
    }

    public ReservationItem(Guid reservationId, Guid productId, int quantity)
    {
        ReservationId = reservationId;
        ProductId = productId;
        Quantity = quantity;
    }
}
