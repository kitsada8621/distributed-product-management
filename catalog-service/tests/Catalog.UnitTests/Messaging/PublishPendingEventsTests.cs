using Catalog.Application.Messaging;
using Catalog.Domain.Messaging;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Messaging;

public class PublishPendingEventsTests
{
    private readonly FakeOutboxRepository _outbox = new();
    private readonly FakeEventPublisher _publisher = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new();

    private DateTime Now => _timeProvider.GetUtcNow().UtcDateTime;

    private Task<int> Run()
    {
        var useCase = new PublishPendingEvents(_outbox, _publisher, _unitOfWork, _timeProvider);
        return useCase.ExecuteAsync(CancellationToken.None);
    }

    private OutboxMessage AddMessage(string key = "order-1", DateTime? createdAt = null)
    {
        var message = OutboxMessage.Create(
            Guid.NewGuid(), "stock.reserved", key, "{\"a\":1}", "corr-1", createdAt ?? Now);
        _outbox.Items.Add(message);
        return message;
    }

    [Fact]
    public async Task AllSucceed_MarksSentAndReturnsCount()
    {
        var first = AddMessage("order-1");
        var second = AddMessage("order-2");

        var sent = await Run();

        Assert.Equal(2, sent);
        Assert.All([first, second], message =>
        {
            Assert.Equal(OutboxStatus.Sent, message.Status);
            Assert.Equal(Now, message.SentAt);
        });
    }

    [Fact]
    public async Task Publisher_ReceivesTopicKeyPayloadAndCorrelationIdUnchanged()
    {
        AddMessage("order-9");

        await Run();

        var published = Assert.Single(_publisher.Published);
        Assert.Equal(new PublishedMessage("stock.reserved", "order-9", "{\"a\":1}", "corr-1"), published);
    }

    [Fact]
    public async Task PublisherFails_StaysPendingWithBackoff()
    {
        var message = AddMessage();
        _publisher.ShouldThrow = true;

        var sent = await Run();

        Assert.Equal(0, sent);
        Assert.Equal(OutboxStatus.Pending, message.Status);
        Assert.Equal(1, message.Attempts);
        Assert.Equal(Now.AddSeconds(1), message.NextAttemptAt);
    }

    [Fact]
    public async Task OneFailure_DoesNotStopTheOthers()
    {
        var failing = AddMessage("order-fail");
        var healthy = AddMessage("order-ok");
        var publisher = new SelectiveFailurePublisher("order-fail");
        var useCase = new PublishPendingEvents(_outbox, publisher, _unitOfWork, _timeProvider);

        var sent = await useCase.ExecuteAsync(CancellationToken.None);

        Assert.Equal(1, sent);
        Assert.Equal(OutboxStatus.Pending, failing.Status);
        Assert.Equal(OutboxStatus.Sent, healthy.Status);
        Assert.Equal(2, _unitOfWork.SaveCount);
    }

    [Fact]
    public async Task SecondFailure_DoublesTheDelay()
    {
        var message = AddMessage();
        _publisher.ShouldThrow = true;
        await Run();
        _timeProvider.UtcNow = _timeProvider.UtcNow.AddSeconds(2);

        await Run();

        Assert.Equal(2, message.Attempts);
        Assert.Equal(Now.AddSeconds(2), message.NextAttemptAt);
    }

    [Fact]
    public async Task NotYetDue_IsSkipped()
    {
        var message = AddMessage();
        message.MarkFailed(Now);

        var sent = await Run();

        Assert.Equal(0, sent);
        Assert.Empty(_publisher.Published);
    }

    [Fact]
    public async Task MoreThanFiftyDue_OnlyFiftyAreHandledPerCall()
    {
        for (var i = 0; i < 60; i++)
        {
            AddMessage($"order-{i}");
        }

        var sent = await Run();

        Assert.Equal(50, sent);
        Assert.Equal(10, _outbox.Items.Count(message => message.Status == OutboxStatus.Pending));
    }

    private sealed class SelectiveFailurePublisher(string failingKey) : IEventPublisher
    {
        public Task PublishAsync(string topic, string key, string payload, string correlationId, CancellationToken cancellationToken)
        {
            if (key == failingKey)
            {
                throw new InvalidOperationException("Kafka did not acknowledge");
            }
            return Task.CompletedTask;
        }
    }
}
