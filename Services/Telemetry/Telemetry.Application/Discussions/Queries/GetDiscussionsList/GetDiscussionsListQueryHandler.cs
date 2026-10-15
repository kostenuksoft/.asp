using MediatR;
using Telemetry.Application.Common.Interfaces;
using Telemetry.Application.Common.Models;

namespace Telemetry.Application.Discussions.Queries.GetDiscussionsList;

public sealed class GetDiscussionsListQueryHandler(IDiscussionQueries queries)
    : IRequestHandler<GetDiscussionsListQuery, PagedResult<DiscussionSummaryDto>>
{
    private const int MaxPageSize = 100;

    public Task<PagedResult<DiscussionSummaryDto>> Handle(
        GetDiscussionsListQuery request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        return queries.GetListAsync(
            request.PackageKey,
            request.State,
            request.Search,
            request.Page,
            Math.Clamp(request.PageSize, 1, MaxPageSize),
            cancellationToken
        );
    }
}
