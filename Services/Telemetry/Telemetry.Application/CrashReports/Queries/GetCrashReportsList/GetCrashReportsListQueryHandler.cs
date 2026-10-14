using MediatR;
using Telemetry.Application.Common.Interfaces;
using Telemetry.Application.Common.Models;

namespace Telemetry.Application.CrashReports.Queries.GetCrashReportsList;

public sealed class GetCrashReportsListQueryHandler(ICrashReportQueries queries)
    : IRequestHandler<GetCrashReportsListQuery, PagedResult<CrashReportSummaryDto>>
{
    private const int MaxPageSize = 100;

    public Task<PagedResult<CrashReportSummaryDto>> Handle(
        GetCrashReportsListQuery request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        return queries.GetListAsync(
            request.PackageKey,
            request.Kind,
            request.Severity,
            request.Page,
            Math.Clamp(request.PageSize, 1, MaxPageSize),
            cancellationToken
        );
    }
}
