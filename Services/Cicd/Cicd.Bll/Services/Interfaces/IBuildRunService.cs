using Cicd.Bll.Dtos;

namespace Cicd.Bll.Services.Interfaces;

public interface IBuildRunService
{
    Task<BuildRunDto> GetByIdAsync(
        long buildRunId,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<BuildRunDto>> GetByPipelineAsync(
        long pipelineId,
        CancellationToken cancellationToken
    );

    Task<RunTimelineDto> GetTimelineAsync(
        long buildRunId,
        CancellationToken cancellationToken
    );

    Task<BuildRunDto> QueueAsync(
        QueueBuildRunDto input,
        CancellationToken cancellationToken
    );

    Task<BuildRunDto> StartAsync(
        long buildRunId,
        StartBuildRunDto input,
        CancellationToken cancellationToken
    );

    Task TransitionAsync(
        long buildRunId,
        TransitionBuildRunDto input,
        CancellationToken cancellationToken
    );

    Task<RunTimelineDto> CompleteAsync(
        long buildRunId,
        CompleteBuildRunDto input,
        CancellationToken cancellationToken
    );

    Task DeleteAsync(
        long buildRunId,
        CancellationToken cancellationToken
    );
}
