using Catalog.Domain.Messaging;

namespace Catalog.UnitTests.Domain;

public class OutboxMessageTests
{
    private static readonly DateTime Now = new(2026, 10, 10, 8, 0, 0, DateTimeKind.Utc);

    private static OutboxMessage CreateMessage()
    {
        return OutboxMessage.Create(Guid.NewGuid(), "stock.reserved", "order-1", "{}", "corr-1", Now);
    }

    [Fact]
    public void Create_IsPendingAndDueNow()
    {
        var id = Guid.NewGuid();

        var message = OutboxMessage.Create(id, "stock.reserved", "order-1", "{}", "corr-1", Now);

        Assert.Equal(id, message.Id);
        Assert.Equal("stock.reserved", message.Topic);
        Assert.Equal("order-1", message.MessageKey);
        Assert.Equal("{}", message.Payload);
        Assert.Equal("corr-1", message.CorrelationId);
        Assert.Equal(OutboxStatus.Pending, message.Status);
        Assert.Equal(0, message.Attempts);
        Assert.Equal(Now, message.NextAttemptAt);
        Assert.Null(message.SentAt);
    }

    [Fact]
    public void MarkSent_SetsSentStatusAndTime()
    {
        var message = CreateMessage();
        var later = Now.AddSeconds(3);

        message.MarkSent(later);

        Assert.Equal(OutboxStatus.Sent, message.Status);
        Assert.Equal(later, message.SentAt);
    }

    [Fact]
    public void MarkFailed_ThreeTimes_DoublesTheDelayEachTime()
    {
        var message = CreateMessage();

        message.MarkFailed(Now);
        Assert.Equal(1, message.Attempts);
        Assert.Equal(Now.AddSeconds(1), message.NextAttemptAt);

        message.MarkFailed(Now);
        Assert.Equal(2, message.Attempts);
        Assert.Equal(Now.AddSeconds(2), message.NextAttemptAt);

        message.MarkFailed(Now);
        Assert.Equal(3, message.Attempts);
        Assert.Equal(Now.AddSeconds(4), message.NextAttemptAt);
        Assert.Equal(OutboxStatus.Pending, message.Status);
    }

    [Fact]
    public void MarkFailed_TenTimes_CapsDelayAtSixtySeconds()
    {
        var message = CreateMessage();

        for (var i = 0; i < 10; i++)
        {
            message.MarkFailed(Now);
        }

        Assert.Equal(10, message.Attempts);
        Assert.Equal(Now.AddSeconds(60), message.NextAttemptAt);
    }
}
