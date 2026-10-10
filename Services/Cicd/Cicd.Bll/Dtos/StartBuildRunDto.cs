using System.ComponentModel.DataAnnotations;

namespace Cicd.Bll.Dtos;

public sealed class StartBuildRunDto
{
    [Required]
    [RegularExpression("^(Windows|Linux|macOS)$", ErrorMessage = "OperatingSystem must be Windows, Linux or macOS.")]
    public string OperatingSystem { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string LeasedBy { get; set; } = string.Empty;
}
