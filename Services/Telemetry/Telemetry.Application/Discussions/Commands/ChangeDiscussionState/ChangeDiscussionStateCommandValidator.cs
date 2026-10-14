using FluentValidation;
using MongoDB.Bson;

namespace Telemetry.Application.Discussions.Commands.ChangeDiscussionState;

public sealed class ChangeDiscussionStateCommandValidator : AbstractValidator<ChangeDiscussionStateCommand>
{
    public ChangeDiscussionStateCommandValidator()
    {
        RuleFor(command => command.Id)
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Id must be a valid ObjectId");
        RuleFor(command => command.State).IsInEnum();
        RuleFor(command => command.Version).GreaterThanOrEqualTo(0);
    }
}
