using MediatR;
using Telemetry.Application.Common.Interfaces;
using Telemetry.Domain.Exceptions;

namespace Telemetry.Application.CrashReports.Queries.GetCrashReportById;

public sealed class GetCrashReportByIdQueryHandler(ICrashReportQueries queries)
    : IRequestHandler<GetCrashReportByIdQuery, CrashReportDto>
{
    public async Task<CrashReportDto> Handle(
        GetCrashReportByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        return await queries.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Crash report {request.Id} was not found.");
    }
}
