using System.Text;
using Catalog.Infrastructure.Kafka;
using Confluent.Kafka;

namespace Catalog.IntegrationTests.Kafka;

public class KafkaMessagesTests
{
    [Fact]
    public void Create_SetsKeyValueAndCorrelationIdHeader()
    {
        var message = KafkaMessages.Create("order-1", "{\"a\":1}", "corr-42");

        Assert.Equal("order-1", message.Key);
        Assert.Equal("{\"a\":1}", message.Value);
        Assert.True(message.Headers.TryGetLastBytes("correlationId", out var bytes));
        Assert.Equal("corr-42", Encoding.UTF8.GetString(bytes));
    }

    [Fact]
    public void ReadCorrelationId_ReturnsTheHeaderValue()
    {
        var message = KafkaMessages.Create("order-1", "{}", "corr-42");

        Assert.Equal("corr-42", KafkaMessages.ReadCorrelationId(message.Headers));
    }

    [Fact]
    public void ReadCorrelationId_MissingHeader_ReturnsFreshGuid()
    {
        var value = KafkaMessages.ReadCorrelationId(new Headers());

        Assert.True(Guid.TryParse(value, out _));
    }

    [Fact]
    public void ReadCorrelationId_NullHeaders_ReturnsFreshGuid()
    {
        Assert.True(Guid.TryParse(KafkaMessages.ReadCorrelationId(null), out _));
    }
}
