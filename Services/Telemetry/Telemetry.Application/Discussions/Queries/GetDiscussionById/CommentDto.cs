namespace Telemetry.Application.Discussions.Queries.GetDiscussionById;

public sealed record CommentDto
{
    public string CommentId { get; init; } = string.Empty;

    public string Author { get; init; } = string.Empty;

    public string Body { get; init; } = string.Empty;

    public DateTime PostedAt { get; init; }

    public DateTime? EditedAt { get; init; }

    public IReadOnlyDictionary<string, int>? Reactions { get; init; }

    public IReadOnlyList<ReplyDto> Replies { get; init; } = [];
}
