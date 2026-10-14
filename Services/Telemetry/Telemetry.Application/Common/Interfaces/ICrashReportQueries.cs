using Telemetry.Application.Common.Models;
using Telemetry.Application.CrashReports.Queries.GetCrashReportById;
using Telemetry.Application.CrashReports.Queries.GetCrashReportsList;
using Telemetry.Domain.Enums;

namespace Telemetry.Application.Common.Interfaces;

public interface ICrashReportQueries
{
    Task<CrashReportDto?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken
    );

    Task<PagedResult<CrashReportSummaryDto>> GetListAsync(
        string? packageKey,
        CrashReportKind? kind,
        CrashSeverity? severity,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
}
