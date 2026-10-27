using Telemetry.Domain.Common;
using Telemetry.Domain.Enums;
using Telemetry.Domain.Exceptions;

namespace Telemetry.Domain.Entities;

public sealed class Discussion : BaseEntity
{
    public const int MinTitleLength = 3;
    public const int MaxTitleLength = 300;
    public const int MaxLabels = 20;
    public const int MaxLabelLength = 50;
    public const int MaxComments = 500;

    private List<Comment> _comments = [];

    private Discussion()
    {
    }

    public Discussion(
        long packageId,
        string packageKey,
        string packageName,
        string publisherSlug,
        string title,
        DiscussionCategory category,
        string openedBy,
        string createdBy
    )
    {
        if (packageId <= 0)
        {
            throw new DomainException("A discussion needs the Id of an existing package.");
        }

        if (string.IsNullOrWhiteSpace(packageName) || string.IsNullOrWhiteSpace(publisherSlug))
        {
            throw new DomainException("Package name and publisher slug are required.");
        }

        if (!string.Equals(packageKey, $"{publisherSlug}/{packageName}", StringComparison.Ordinal))
        {
            throw new DomainException("Package key must be '<publisher>/<name>' of the same package.");
        }

        if (string.IsNullOrWhiteSpace(openedBy) || openedBy.Length > Comment.MaxAuthorLength)
        {
            throw new DomainException($"The author is required and has at most {Comment.MaxAuthorLength} characters.");
        }

        PackageId = packageId;
        PackageKey = packageKey;
        PackageName = packageName;
        PublisherSlug = publisherSlug;
        Title = RequireTitle(title);
        Category = RequireCategory(category);
        State = DiscussionState.Open;
        OpenedBy = openedBy;
        OpenedAt = CreatedAt;
        CreatedBy = createdBy;
    }

    public long PackageId { get; private set; }

    public string PackageKey { get; private set; } = string.Empty;

    public string PackageName { get; private set; } = string.Empty;

    public string PublisherSlug { get; private set; } = string.Empty;

    public DateTime? PackageSyncedAt { get; private set; }

    public string Title { get; private set; } = string.Empty;

    public DiscussionCategory Category { get; private set; }

    public DiscussionState State { get; private set; }

    public IReadOnlyList<string> Labels { get; private set; } = [];

    public IReadOnlyList<string> AffectedVersions { get; private set; } = [];

    public IReadOnlyList<long> LinkedBuildRunIds { get; private set; } = [];

    public string OpenedBy { get; private set; } = string.Empty;

    public DateTime OpenedAt { get; private set; }

    public DateTime? ClosedAt { get; private set; }

    public int CommentCount { get; private set; }

    public IReadOnlyCollection<Comment> Comments => _comments.AsReadOnly();

    public void Edit(
        string title,
        DiscussionCategory category,
        IReadOnlyCollection<string> labels
    )
    {
        ArgumentNullException.ThrowIfNull(labels);
        ThrowIfClosed();

        if (labels.Count > MaxLabels || !labels.All(IsLabel))
        {
            throw new DomainException($"At most {MaxLabels} labels of 1 to {MaxLabelLength} characters.");
        }

        Title = RequireTitle(title);
        Category = RequireCategory(category);
        Labels = [.. labels.Distinct(StringComparer.Ordinal)];
        Touch();
    }

    public Comment AddComment(
        string author,
        string body
    )
    {
        ThrowIfClosed();

        if (_comments.Count >= MaxComments)
        {
            throw new DomainException($"A discussion holds at most {MaxComments} comments.");
        }

        Comment comment = new($"c{_comments.Count + 1}", author, body, DateTime.UtcNow);
        _comments.Add(comment);
        CommentCount = _comments.Count;
        Touch();
        return comment;
    }

    public void ChangeState(DiscussionState target)
    {
        bool allowed = (State, target) switch
        {
            (DiscussionState.Open, DiscussionState.Answered) => true,
            (DiscussionState.Open, DiscussionState.Closed) => true,
            (DiscussionState.Answered, DiscussionState.Open) => true,
            (DiscussionState.Answered, DiscussionState.Closed) => true,
            (DiscussionState.Closed, DiscussionState.Open) => true,
            _ => false,
        };

        if (!allowed)
        {
            throw new DomainException($"A discussion cannot move from {State} to {target}.");
        }

        State = target;
        ClosedAt = target == DiscussionState.Closed ? DateTime.UtcNow : null;
        Touch();
    }

    private void ThrowIfClosed()
    {
        if (State == DiscussionState.Closed)
        {
            throw new DomainException("A closed discussion cannot be changed; reopen it first.");
        }
    }

    private static string RequireTitle(string title)
    {
        if (string.IsNullOrWhiteSpace(title) || title.Length < MinTitleLength || title.Length > MaxTitleLength)
        {
            throw new DomainException($"Title must have {MinTitleLength} to {MaxTitleLength} characters.");
        }

        return title;
    }

    private static bool IsLabel(string label) => !string.IsNullOrWhiteSpace(label) && label.Length <= MaxLabelLength;

    private static DiscussionCategory RequireCategory(DiscussionCategory category)
    {
        if (!Enum.IsDefined(category))
        {
            throw new DomainException($"Unknown discussion category {category}.");
        }

        return category;
    }
}
