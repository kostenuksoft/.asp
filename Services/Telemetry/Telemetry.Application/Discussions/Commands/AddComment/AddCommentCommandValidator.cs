using FluentValidation;
using MongoDB.Bson;
using Telemetry.Domain.Entities;

namespace Telemetry.Application.Discussions.Commands.AddComment;

public sealed class AddCommentCommandValidator : AbstractValidator<AddCommentCommand>
{
    public AddCommentCommandValidator()
    {
        RuleFor(command => command.Id)
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Id must be a valid ObjectId");
        RuleFor(command => command.Author).NotEmpty().MaximumLength(Comment.MaxAuthorLength);
        RuleFor(command => command.Body).NotEmpty().MaximumLength(Comment.MaxBodyLength);
        RuleFor(command => command.Version).GreaterThanOrEqualTo(0);
    }
}
