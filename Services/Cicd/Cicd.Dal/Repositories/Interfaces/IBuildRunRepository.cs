using Cicd.Domain.Models;

namespace Cicd.Dal.Repositories.Interfaces;

public interface IBuildRunRepository : IGenericRepository<BuildRun>
{
    Task<BuildRun?> GetWithDetailAndStepsAsync(
        long buildRunId,
        CancellationToken cancellationToken = default
    );

    Task<IReadOnlyList<BuildRun>> GetByPipelineAsync(
        long pipelineId,
        CancellationToken cancellationToken = default
    );

    Task<RunTimeline?> GetTimelineAsync(
        long buildRunId,
        CancellationToken cancellationToken = default
    );

    Task<long> QueueAsync(
        BuildRun run,
        BuildRunDetail detail,
        CancellationToken cancellationToken = default
    );

    Task TransitionStatusAsync(
        long buildRunId,
        string toStatus,
        string? reason,
        string changedBy,
        byte[]? expectedRowVersion,
        CancellationToken cancellationToken = default
    );
}
