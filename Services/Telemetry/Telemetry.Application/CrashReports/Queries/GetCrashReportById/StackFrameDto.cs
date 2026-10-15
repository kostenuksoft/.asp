namespace Telemetry.Application.CrashReports.Queries.GetCrashReportById;

public sealed record StackFrameDto
{
    public string Method { get; init; } = string.Empty;

    public string? File { get; init; }

    public int? Line { get; init; }
}
