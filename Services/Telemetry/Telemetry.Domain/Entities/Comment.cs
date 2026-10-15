using Telemetry.Domain.Exceptions;

namespace Telemetry.Domain.Entities;

public sealed class Comment
{
    public const int MaxAuthorLength = 128;
    public const int MaxBodyLength = 8000;

    private List<Reply> _replies = [];

    private Comment()
    {
    }

    internal Comment(
        string commentId,
        string author,
        string body,
        DateTime postedAt
    )
    {
        if (string.IsNullOrWhiteSpace(author) || author.Length > MaxAuthorLength)
        {
            throw new DomainException($"Comment author is required and has at most {MaxAuthorLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(body) || body.Length > MaxBodyLength)
        {
            throw new DomainException($"Comment text is required and has at most {MaxBodyLength} characters.");
        }

        CommentId = commentId;
        Author = author;
        Body = body;
        PostedAt = postedAt;
    }

    public string CommentId { get; private set; } = string.Empty;

    public string Author { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public DateTime PostedAt { get; private set; }

    public DateTime? EditedAt { get; private set; }

    public IReadOnlyDictionary<string, int>? Reactions { get; private set; }

    public IReadOnlyCollection<Reply> Replies => _replies.AsReadOnly();
}
