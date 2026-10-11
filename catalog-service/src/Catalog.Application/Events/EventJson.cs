using System.Text.Json;

namespace Catalog.Application.Events;

public static class EventJson
{
    // Web defaults: camelCase names on write, case-insensitive on read (API-SPEC §7.1).
    private static readonly JsonSerializerOptions Options = new(JsonSerializerDefaults.Web);

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, Options);
    }

    public static T Deserialize<T>(string json)
    {
        return JsonSerializer.Deserialize<T>(json, Options)
            ?? throw new JsonException("Event JSON is null");
    }
}
