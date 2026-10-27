using System.ComponentModel.DataAnnotations;

namespace Cicd.Bll.Dtos;

public sealed class TransitionBuildRunDto
{
    [Required]
    [RegularExpression(
        "^(Queued|Running|Succeeded|Failed|Cancelled)$",
        ErrorMessage = "ToStatus must be a build run status."
    )]
    public string ToStatus { get; set; } = string.Empty;

    [StringLength(400)]
    public string? Reason { get; set; }

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string ChangedBy { get; set; } = string.Empty;

    public string? ExpectedRowVersion { get; set; }
}
