using System.Text.Json;

namespace Platform.EventBus;

public static class EventBusJson
{
    public static JsonSerializerOptions Options { get; } = new(JsonSerializerDefaults.Web);
}
