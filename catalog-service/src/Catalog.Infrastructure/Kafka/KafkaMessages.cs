using System.Text;
using Confluent.Kafka;

namespace Catalog.Infrastructure.Kafka;

public static class KafkaMessages
{
    public const string CorrelationIdHeader = "correlationId";

    public static Message<string, string> Create(string key, string payload, string correlationId)
    {
        var headers = new Headers { { CorrelationIdHeader, Encoding.UTF8.GetBytes(correlationId) } };
        return new Message<string, string> { Key = key, Value = payload, Headers = headers };
    }

    public static string ReadCorrelationId(Headers? headers)
    {
        if (headers is not null && headers.TryGetLastBytes(CorrelationIdHeader, out var bytes))
        {
            return Encoding.UTF8.GetString(bytes);
        }

        return Guid.NewGuid().ToString();
    }
}
