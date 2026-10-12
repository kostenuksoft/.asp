using Cicd.Bll.Dtos;

namespace Cicd.Bll.Services.Interfaces;

public interface IPipelineService
{
    Task<IReadOnlyList<PipelineDto>> GetAllAsync(CancellationToken cancellationToken);

    Task<PipelineDto> GetByIdAsync(
        long pipelineId,
        CancellationToken cancellationToken
    );

    Task<PipelineDto> CreateAsync(
        CreatePipelineDto input,
        CancellationToken cancellationToken
    );

    Task DeleteAsync(
        long pipelineId,
        CancellationToken cancellationToken
    );
}
