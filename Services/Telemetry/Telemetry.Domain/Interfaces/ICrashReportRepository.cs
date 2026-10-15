using Telemetry.Domain.Entities;

namespace Telemetry.Domain.Interfaces;

public interface ICrashReportRepository
{
    Task AddAsync(
        CrashReport report,
        CancellationToken cancellationToken = default
    );
}
