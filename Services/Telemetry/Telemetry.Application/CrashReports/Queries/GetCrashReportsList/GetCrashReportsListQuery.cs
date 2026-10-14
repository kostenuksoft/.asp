using Telemetry.Application.Common.Messaging;
using Telemetry.Application.Common.Models;
using Telemetry.Domain.Enums;

namespace Telemetry.Application.CrashReports.Queries.GetCrashReportsList;

public sealed record GetCrashReportsListQuery(
    string? PackageKey,
    CrashReportKind? Kind,
    CrashSeverity? Severity,
    int Page = 1,
    int PageSize = 20
) : IQuery<PagedResult<CrashReportSummaryDto>>;
