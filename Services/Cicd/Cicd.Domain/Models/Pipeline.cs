namespace Cicd.Domain.Models;

public sealed class Pipeline : AuditableEntity
{
    public long PipelineId { get; set; }

    public string Name { get; set; } = string.Empty;

    public string RepositoryUrl { get; set; } = string.Empty;

    public string DefaultBranch { get; set; } = string.Empty;

    public long? PackageId { get; set; }

    public string? PackageKey { get; set; }

    public bool IsEnabled { get; set; }

    public byte[] RowVersion { get; set; } = [];
}
