using FluentValidation;
using MongoDB.Bson;
using Telemetry.Domain.Entities;

namespace Telemetry.Application.Discussions.Commands.UpdateDiscussion;

public sealed class UpdateDiscussionCommandValidator : AbstractValidator<UpdateDiscussionCommand>
{
    public UpdateDiscussionCommandValidator()
    {
        RuleFor(command => command.Id)
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Id must be a valid ObjectId");
        RuleFor(command => command.Title).NotEmpty().Length(Discussion.MinTitleLength, Discussion.MaxTitleLength);
        RuleFor(command => command.Category).IsInEnum();
        RuleFor(command => command.Labels)
            .NotNull()
            .Must(labels => labels.Count <= Discussion.MaxLabels)
            .WithMessage($"At most {Discussion.MaxLabels} labels.");
        RuleForEach(command => command.Labels).NotEmpty().MaximumLength(Discussion.MaxLabelLength);
        RuleFor(command => command.Version).GreaterThanOrEqualTo(0);
    }
}
