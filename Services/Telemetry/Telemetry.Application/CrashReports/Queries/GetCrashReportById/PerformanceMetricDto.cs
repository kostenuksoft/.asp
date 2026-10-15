namespace Telemetry.Application.CrashReports.Queries.GetCrashReportById;

public sealed record PerformanceMetricDto
{
    public string Name { get; init; } = string.Empty;

    public double Observed { get; init; }

    public double Threshold { get; init; }

    public string? Unit { get; init; }
}
