namespace Cicd.Bll.Dtos;

public sealed record PipelineDto(
    long PipelineId,
    string Name,
    string RepositoryUrl,
    string DefaultBranch,
    long? PackageId,
    string? PackageKey,
    bool IsEnabled);
