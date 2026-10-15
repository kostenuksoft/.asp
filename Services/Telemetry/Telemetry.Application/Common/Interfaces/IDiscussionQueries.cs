using Telemetry.Application.Common.Models;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;
using Telemetry.Application.Discussions.Queries.GetDiscussionsList;
using Telemetry.Domain.Enums;

namespace Telemetry.Application.Common.Interfaces;

public interface IDiscussionQueries
{
    Task<DiscussionDetailsDto?> GetByIdAsync(
        string id,
        CancellationToken cancellationToken
    );

    Task<PagedResult<DiscussionSummaryDto>> GetListAsync(
        string? packageKey,
        DiscussionState? state,
        string? search,
        int page,
        int pageSize,
        CancellationToken cancellationToken
    );
}
