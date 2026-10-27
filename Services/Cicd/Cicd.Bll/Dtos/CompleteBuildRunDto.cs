using System.ComponentModel.DataAnnotations;

namespace Cicd.Bll.Dtos;

public sealed class CompleteBuildRunDto
{
    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string CompletedBy { get; set; } = string.Empty;

    [Required]
    [MinLength(1)]
    public List<ProducedArtifactDto> Artifacts { get; set; } = [];
}
