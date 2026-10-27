using AutoMapper;
using Cicd.Bll.Dtos;
using Cicd.Bll.Services.Interfaces;
using Cicd.Dal.UnitOfWork;
using Cicd.Domain.Exceptions;
using Cicd.Domain.Models;

namespace Cicd.Bll.Services;

public sealed class PipelineService(
    IUnitOfWork unitOfWork,
    IMapper mapper
)
    : IPipelineService
{
    public async Task<IReadOnlyList<PipelineDto>> GetAllAsync(CancellationToken cancellationToken)
    {
        IReadOnlyList<Pipeline> pipelines = await unitOfWork.Pipelines.GetAllAsync(cancellationToken);
        return mapper.Map<IReadOnlyList<PipelineDto>>(pipelines);
    }

    public async Task<PipelineDto> GetByIdAsync(
        long pipelineId,
        CancellationToken cancellationToken
    )
    {
        Pipeline pipeline = await unitOfWork.Pipelines.GetByIdAsync(pipelineId, cancellationToken)
            ?? throw new NotFoundException($"Pipeline {pipelineId} was not found.");
        return mapper.Map<PipelineDto>(pipeline);
    }

    public async Task<PipelineDto> CreateAsync(
        CreatePipelineDto input,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(input);

        if (input.PackageId is null != input.PackageKey is null)
        {
            throw new ValidationException("PackageId and PackageKey must be given together or both omitted.");
        }

        Pipeline pipeline = new()
        {
            Name = input.Name,
            RepositoryUrl = input.RepositoryUrl,
            DefaultBranch = input.DefaultBranch,
            PackageId = input.PackageId,
            PackageKey = input.PackageKey,
            IsEnabled = input.IsEnabled,
            CreatedBy = "cicd-api",
        };

        long pipelineId = await unitOfWork.Pipelines.AddAsync(pipeline, cancellationToken);
        return await GetByIdAsync(pipelineId, cancellationToken);
    }

    public async Task DeleteAsync(
        long pipelineId,
        CancellationToken cancellationToken
    )
    {
        if (!await unitOfWork.Pipelines.DeleteAsync(pipelineId, cancellationToken))
        {
            throw new NotFoundException($"Pipeline {pipelineId} was not found.");
        }
    }
}
