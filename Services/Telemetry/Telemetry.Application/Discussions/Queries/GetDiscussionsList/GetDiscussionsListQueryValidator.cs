using FluentValidation;

namespace Telemetry.Application.Discussions.Queries.GetDiscussionsList;

public sealed class GetDiscussionsListQueryValidator : AbstractValidator<GetDiscussionsListQuery>
{
    public GetDiscussionsListQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.State).IsInEnum();
        RuleFor(query => query.PackageKey).MaximumLength(300);
        RuleFor(query => query.Search).MaximumLength(100);
    }
}
