using System.Diagnostics.Metrics;

namespace Cicd.Api.Metrics;

public sealed class CicdMetrics
{
    public const string MeterName = "Cicd.Api";

    private readonly Counter<long> _runsQueued;
    private readonly Counter<long> _runsCompleted;
    private readonly Histogram<double> _operationDuration;

    public CicdMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        Meter meter = meterFactory.Create(MeterName);
        _runsQueued = meter.CreateCounter<long>(
            "build_runs.queued",
            unit: "{run}",
            description: "Build runs put in the queue"
        );
        _runsCompleted = meter.CreateCounter<long>(
            "build_runs.completed",
            unit: "{run}",
            description: "Build runs finished with a terminal status"
        );
        _operationDuration = meter.CreateHistogram<double>(
            "build_runs.operation.duration",
            unit: "ms",
            description: "Duration of build run operations"
        );
    }

    public void RunQueued(string triggerKind) => _runsQueued.Add(
        1,
        new KeyValuePair<string, object?>("trigger.kind", triggerKind)
    );

    public void RunCompleted(string status) => _runsCompleted.Add(
        1,
        new KeyValuePair<string, object?>("run.status", status)
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
