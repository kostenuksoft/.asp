using System.Globalization;
using System.Text;

namespace Platform.EventBus;

public static class EventBusHeaders
{
    public const string CorrelationId = "x-correlation-id";
    public const string SchemaVersion = "x-schema-version";
    public const string RetryCount = "x-retry-count";

    public static string? ReadString(
        IDictionary<string, object?>? headers,
        string key
    ) => headers is not null && headers.TryGetValue(key, out object? value)
        ? value switch
        {
            byte[] bytes => Encoding.UTF8.GetString(bytes),
            string text => text,
            _ => null,
        }
        : null;

    public static int ReadInt(
        IDictionary<string, object?>? headers,
        string key,
        int defaultValue
    ) => headers is not null && headers.TryGetValue(key, out object? value)
        ? value switch
        {
            int number => number,
            long number => (int)number,
            byte number => number,
            short number => number,
            byte[] bytes when int.TryParse(Encoding.UTF8.GetString(bytes), NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed) => parsed,
            _ => defaultValue,
        }
        : defaultValue;
}
