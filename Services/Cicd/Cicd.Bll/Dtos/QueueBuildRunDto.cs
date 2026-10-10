using System.ComponentModel.DataAnnotations;

namespace Cicd.Bll.Dtos;

public sealed class QueueBuildRunDto
{
    [Range(1, long.MaxValue)]
    public long PipelineId { get; set; }

    [Required]
    [RegularExpression("^[0-9a-f]{40}$", ErrorMessage = "CommitSha must be 40 lowercase hexadecimal characters.")]
    public string CommitSha { get; set; } = string.Empty;

    [Required]
    [StringLength(1000, MinimumLength = 1)]
    public string CommitMessage { get; set; } = string.Empty;

    [Required]
    [StringLength(200, MinimumLength = 1)]
    public string BranchName { get; set; } = string.Empty;

    [Required]
    [StringLength(128, MinimumLength = 1)]
    public string TriggeredBy { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(Push|PullRequest|Manual|Schedule|Api)$", ErrorMessage = "TriggerKind must be Push, PullRequest, Manual, Schedule or Api.")]
    public string TriggerKind { get; set; } = string.Empty;

    public string? TriggerPayload { get; set; }
}
