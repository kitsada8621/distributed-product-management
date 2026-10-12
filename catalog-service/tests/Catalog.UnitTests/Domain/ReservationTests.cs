using Catalog.Domain.Reservations;

namespace Catalog.UnitTests.Domain;

public class ReservationTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Reserve_CreatesReservedStatusWithItems()
    {
        var orderId = Guid.NewGuid();
        var productA = Guid.NewGuid();
        var productB = Guid.NewGuid();

        var reservation = Reservation.Reserve(orderId, [(productA, 2), (productB, 5)], Now);

        Assert.Equal(orderId, reservation.OrderId);
        Assert.Equal(ReservationStatus.Reserved, reservation.Status);
        Assert.Equal(2, reservation.Items.Count);
        Assert.Contains(reservation.Items, item => item.ProductId == productA && item.Quantity == 2);
        Assert.Contains(reservation.Items, item => item.ProductId == productB && item.Quantity == 5);
        Assert.All(reservation.Items, item => Assert.Equal(orderId, item.ReservationId));
    }

    [Fact]
    public void Commit_FromReserved_ChangesToCommitted()
    {
        var reservation = Reservation.Reserve(Guid.NewGuid(), [(Guid.NewGuid(), 1)], Now);
        var later = Now.AddMinutes(1);

        var changed = reservation.Commit(later);

        Assert.True(changed);
        Assert.Equal(ReservationStatus.Committed, reservation.Status);
        Assert.Equal(later, reservation.UpdatedAt);
    }

    [Fact]
    public void Commit_FromCommitted_ChangesNothing()
    {
        var reservation = Reservation.Reserve(Guid.NewGuid(), [(Guid.NewGuid(), 1)], Now);
        reservation.Commit(Now);

        var changed = reservation.Commit(Now.AddMinutes(1));

        Assert.False(changed);
        Assert.Equal(ReservationStatus.Committed, reservation.Status);
    }

    [Fact]
    public void Commit_FromReleased_ChangesNothing()
    {
        var reservation = Reservation.CreateTombstone(Guid.NewGuid(), Now);

        var changed = reservation.Commit(Now.AddMinutes(1));

        Assert.False(changed);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
    }

    [Fact]
    public void Release_FromReserved_ChangesToReleased()
    {
        var reservation = Reservation.Reserve(Guid.NewGuid(), [(Guid.NewGuid(), 1)], Now);
        var later = Now.AddMinutes(1);

        var changed = reservation.Release(later);

        Assert.True(changed);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Equal(later, reservation.UpdatedAt);
    }

    [Fact]
    public void Release_FromCommitted_ChangesNothing()
    {
        var reservation = Reservation.Reserve(Guid.NewGuid(), [(Guid.NewGuid(), 1)], Now);
        reservation.Commit(Now);

        var changed = reservation.Release(Now.AddMinutes(1));

        Assert.False(changed);
        Assert.Equal(ReservationStatus.Committed, reservation.Status);
    }

    [Fact]
    public void Release_FromReleased_ChangesNothing()
    {
        var reservation = Reservation.CreateTombstone(Guid.NewGuid(), Now);

        var changed = reservation.Release(Now.AddMinutes(1));

        Assert.False(changed);
    }

    [Fact]
    public void CreateTombstone_IsReleasedWithNoItems()
    {
        var orderId = Guid.NewGuid();

        var reservation = Reservation.CreateTombstone(orderId, Now);

        Assert.Equal(orderId, reservation.OrderId);
        Assert.Equal(ReservationStatus.Released, reservation.Status);
        Assert.Empty(reservation.Items);
    }
}
