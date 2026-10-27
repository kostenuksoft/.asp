using System.ComponentModel.DataAnnotations;

namespace Cicd.Bll.Dtos;

public sealed class CreatePipelineDto
{
    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string Name { get; set; } = string.Empty;

    [Required]
    [StringLength(400)]
    [RegularExpression(
        "^(https?://|git@).+",
        ErrorMessage = "RepositoryUrl must start with http://, https:// or git@."
    )]
    public string RepositoryUrl { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string DefaultBranch { get; set; } = "main";

    [Range(1, long.MaxValue)]
    public long? PackageId { get; set; }

    [StringLength(300)]
    [RegularExpression(
        "^[a-z0-9-]{2,}/[A-Za-z0-9._-]{2,}$",
        ErrorMessage = "PackageKey must look like publisher/package."
    )]
    public string? PackageKey { get; set; }

    public bool IsEnabled { get; set; } = true;
}
