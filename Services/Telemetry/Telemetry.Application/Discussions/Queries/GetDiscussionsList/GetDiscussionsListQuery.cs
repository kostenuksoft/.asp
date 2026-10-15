using Telemetry.Application.Common.Messaging;
using Telemetry.Application.Common.Models;
using Telemetry.Domain.Enums;

namespace Telemetry.Application.Discussions.Queries.GetDiscussionsList;

public sealed record GetDiscussionsListQuery(
    string? PackageKey,
    DiscussionState? State,
    string? Search,
    int Page = 1,
    int PageSize = 20
) : IQuery<PagedResult<DiscussionSummaryDto>>;
