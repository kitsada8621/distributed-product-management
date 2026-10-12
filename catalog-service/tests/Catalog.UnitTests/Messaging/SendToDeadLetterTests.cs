using System.Text.Json;
using Catalog.Application.Messaging;
using Catalog.Domain.Messaging;
using Catalog.UnitTests.Fakes;

namespace Catalog.UnitTests.Messaging;

public class SendToDeadLetterTests
{
    private readonly FakeOutboxRepository _outbox = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeTimeProvider _timeProvider = new();

    private async Task<OutboxMessage> RunAsync(string originalMessage = "{\"broken\":true}")
    {
        var useCase = new SendToDeadLetter(_outbox, _unitOfWork, _timeProvider);
        var request = new DeadLetterRequest("order.created", "order-1", originalMessage, "corr-5", "boom", 3);
        await useCase.ExecuteAsync(request, CancellationToken.None);
        return Assert.Single(_outbox.Items);
    }

    [Fact]
    public async Task Topic_IsDlqPrefixedOriginalTopic()
    {
        var message = await RunAsync();

        Assert.Equal("dlq.order.created", message.Topic);
    }

    [Fact]
    public async Task KeyAndCorrelationId_AreKept()
    {
        var message = await RunAsync();

        Assert.Equal("order-1", message.MessageKey);
        Assert.Equal("corr-5", message.CorrelationId);
        Assert.Equal(OutboxStatus.Pending, message.Status);
    }

    [Fact]
    public async Task Payload_HoldsRawOriginalMessageErrorAndAttempts()
    {
        var message = await RunAsync("{\"broken\":true}");

        using var document = JsonDocument.Parse(message.Payload);
        var root = document.RootElement;
        Assert.Equal("order.created", root.GetProperty("originalTopic").GetString());
        Assert.Equal("{\"broken\":true}", root.GetProperty("originalMessage").GetString());
        Assert.Equal("boom", root.GetProperty("error").GetString());
        Assert.Equal(3, root.GetProperty("attempts").GetInt32());
    }

    [Fact]
    public async Task Execute_SavesOnce()
    {
        await RunAsync();

        Assert.Equal(1, _unitOfWork.SaveCount);
    }
}
