using System.ComponentModel.DataAnnotations;

namespace Cicd.Bll.Dtos;

public sealed class ProducedArtifactDto
{
    [Required]
    [StringLength(50, MinimumLength = 1)]
    public string SemVer { get; set; } = string.Empty;

    [Range(1, long.MaxValue)]
    public long SizeBytes { get; set; }

    [Required]
    [RegularExpression("^[0-9a-f]{64}$", ErrorMessage = "Sha256 must be 64 lowercase hexadecimal characters.")]
    public string Sha256 { get; set; } = string.Empty;

    [Required]
    [StringLength(500, MinimumLength = 1)]
    public string StorageUri { get; set; } = string.Empty;
}
