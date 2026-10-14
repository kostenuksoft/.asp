using FluentValidation;
using MongoDB.Bson;

namespace Telemetry.Application.Discussions.Queries.GetDiscussionById;

public sealed class GetDiscussionByIdQueryValidator : AbstractValidator<GetDiscussionByIdQuery>
{
    public GetDiscussionByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Id must be a valid ObjectId");
    }
}
