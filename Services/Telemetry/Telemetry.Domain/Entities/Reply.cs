namespace Telemetry.Domain.Entities;

public sealed class Reply
{
    private Reply()
    {
    }

    public string CommentId { get; private set; } = string.Empty;

    public string Author { get; private set; } = string.Empty;

    public string Body { get; private set; } = string.Empty;

    public DateTime PostedAt { get; private set; }
}
