using MongoDB.Bson;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Exceptions;
using Telemetry.Domain.Interfaces;

namespace Telemetry.Application.Discussions.Commands;

internal static class DiscussionRepositoryExtensions
{
    public static async Task<Discussion> GetAtVersionAsync(
        this IDiscussionRepository discussions,
        string id,
        int expectedVersion,
        CancellationToken cancellationToken
    )
    {
        Discussion discussion = await discussions.GetByIdAsync(ObjectId.Parse(id), cancellationToken)
            ?? throw new NotFoundException($"Discussion {id} was not found.");

        if (discussion.Version != expectedVersion)
        {
            throw new ConflictException($"Discussion {id} is at version {discussion.Version}, not {expectedVersion}.");
        }

        return discussion;
    }
}
