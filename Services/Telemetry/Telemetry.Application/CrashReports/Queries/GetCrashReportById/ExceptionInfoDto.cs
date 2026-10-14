namespace Telemetry.Application.CrashReports.Queries.GetCrashReportById;

public sealed record ExceptionInfoDto
{
    public string Type { get; init; } = string.Empty;

    public string Message { get; init; } = string.Empty;

    public string? InnerType { get; init; }
}
