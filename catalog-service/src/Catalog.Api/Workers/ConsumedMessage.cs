namespace Catalog.Api.Workers;

public sealed record ConsumedMessage(string Topic, string Key, string Value, string CorrelationId);
