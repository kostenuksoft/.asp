namespace Telemetry.Application.Discussions.Queries.GetDiscussionById;

public sealed record ReplyDto
{
    public string CommentId { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public DateTime PostedAt { get; init; }
}
