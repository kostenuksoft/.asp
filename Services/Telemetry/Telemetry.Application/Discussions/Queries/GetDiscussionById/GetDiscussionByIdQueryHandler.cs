using MediatR;
using Telemetry.Application.Common.Interfaces;
using Telemetry.Domain.Exceptions;

namespace Telemetry.Application.Discussions.Queries.GetDiscussionById;

public sealed class GetDiscussionByIdQueryHandler(IDiscussionQueries queries)
    : IRequestHandler<GetDiscussionByIdQuery, DiscussionDetailsDto>
{
    public async Task<DiscussionDetailsDto> Handle(
        GetDiscussionByIdQuery request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        return await queries.GetByIdAsync(request.Id, cancellationToken)
            ?? throw new NotFoundException($"Discussion {request.Id} was not found.");
    }
}
