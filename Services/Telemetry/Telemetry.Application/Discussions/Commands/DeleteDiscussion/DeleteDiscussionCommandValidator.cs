using FluentValidation;
using MongoDB.Bson;

namespace Telemetry.Application.Discussions.Commands.DeleteDiscussion;

public sealed class DeleteDiscussionCommandValidator : AbstractValidator<DeleteDiscussionCommand>
{
    public DeleteDiscussionCommandValidator()
    {
        RuleFor(command => command.Id)
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Id must be a valid ObjectId");
        RuleFor(command => command.Version).GreaterThanOrEqualTo(0);
    }
}
