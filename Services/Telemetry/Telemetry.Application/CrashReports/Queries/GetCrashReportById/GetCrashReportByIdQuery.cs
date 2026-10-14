using Telemetry.Application.Common.Messaging;

namespace Telemetry.Application.CrashReports.Queries.GetCrashReportById;

public sealed record GetCrashReportByIdQuery(string Id) : IQuery<CrashReportDto>;
