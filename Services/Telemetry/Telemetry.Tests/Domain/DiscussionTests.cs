using Telemetry.Domain.Entities;
using Telemetry.Domain.Enums;
using Telemetry.Domain.Exceptions;
using Xunit;

namespace Telemetry.Tests.Domain;

public sealed class DiscussionTests
{
    [Fact]
    public void DiscussionWithoutPackageThrowsDomainExceptionWithoutDatabase()
    {
        Assert.Throws<DomainException>(() => CreateDiscussion(packageId: 0));
    }

    [Fact]
    public void PackageKeyMustBeComposedOfPublisherAndName()
    {
        Assert.Throws<DomainException>(() => new Discussion(
            2,
            "platform-team/devhub-storage",
            "devhub-auth",
            "platform-team",
            "Wrong package key",
            DiscussionCategory.Bug,
            "m.shevchenko",
            "tests"
        ));
    }

    [Fact]
    public void AddCommentNumbersCommentsAndKeepsTheCount()
    {
        Discussion discussion = CreateDiscussion();

        discussion.AddComment("o.kovalenko", "First answer");
        Comment second = discussion.AddComment("m.shevchenko", "Thanks");

        Assert.Equal("c2", second.CommentId);
        Assert.Equal(2, discussion.CommentCount);
        Assert.NotNull(discussion.UpdatedAt);
    }

    [Fact]
    public void ClosedDiscussionRejectsComments()
    {
        Discussion discussion = CreateDiscussion();
        discussion.ChangeState(DiscussionState.Closed);

        Assert.Throws<DomainException>(() => discussion.AddComment("o.kovalenko", "Too late"));
    }

    [Fact]
    public void ClosedDiscussionCannotBecomeAnswered()
    {
        Discussion discussion = CreateDiscussion();
        discussion.ChangeState(DiscussionState.Closed);

        Assert.Throws<DomainException>(() => discussion.ChangeState(DiscussionState.Answered));
        Assert.NotNull(discussion.ClosedAt);
    }

    private static Discussion CreateDiscussion(long packageId = 2) => new(
        packageId,
        "platform-team/devhub-auth",
        "devhub-auth",
        "platform-team",
        "Token refresh fails behind a proxy",
        DiscussionCategory.Bug,
        "m.shevchenko",
        "tests"
    );
}
