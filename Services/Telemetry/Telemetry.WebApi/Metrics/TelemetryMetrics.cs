using System.Diagnostics.Metrics;
using System.Text.Json;
using Telemetry.Domain.Enums;

namespace Telemetry.WebApi.Metrics;

public sealed class TelemetryMetrics
{
    public const string MeterName = "Telemetry.WebApi";

    private readonly Counter<long> _discussionsCreated;
    private readonly Histogram<double> _operationDuration;

    public TelemetryMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        Meter meter = meterFactory.Create(MeterName);
        _discussionsCreated = meter.CreateCounter<long>("discussions.created", unit: "{discussion}", description: "Discussions opened for packages");
        _operationDuration = meter.CreateHistogram<double>("discussions.operation.duration", unit: "ms", description: "Duration of discussion write operations");
    }

    public void DiscussionCreated(DiscussionCategory category) => _discussionsCreated.Add(
        1,
        new KeyValuePair<string, object?>("discussion.category", JsonNamingPolicy.CamelCase.ConvertName(category.ToString()))
    );

    public void RecordDuration(
        string operation,
        bool success,
        double milliseconds
    ) => _operationDuration.Record(
        milliseconds,
        new KeyValuePair<string, object?>("operation", operation),
        new KeyValuePair<string, object?>("status", success ? "success" : "failure")
    );
}
