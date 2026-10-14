using FluentValidation;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Interfaces;

namespace Telemetry.Application.Discussions.Commands.CreateDiscussion;

public sealed class CreateDiscussionCommandValidator : AbstractValidator<CreateDiscussionCommand>
{
    public CreateDiscussionCommandValidator(IDiscussionRepository discussions)
    {
        RuleFor(command => command.PackageId).GreaterThan(0);
        RuleFor(command => command.PublisherSlug).NotEmpty().MaximumLength(100);
        RuleFor(command => command.PackageName).NotEmpty().MaximumLength(200);
        RuleFor(command => command.PackageKey)
            .Equal(command => $"{command.PublisherSlug}/{command.PackageName}")
            .WithMessage("PackageKey must be '<publisherSlug>/<packageName>'.");
        RuleFor(command => command.Title).NotEmpty().Length(Discussion.MinTitleLength, Discussion.MaxTitleLength);
        RuleFor(command => command.Category).IsInEnum();
        RuleFor(command => command.OpenedBy).NotEmpty().MaximumLength(Comment.MaxAuthorLength);

        RuleFor(command => command)
            .MustAsync(async (command, cancellationToken) =>
                !await discussions.ExistsAsync(command.PackageKey, command.Title, cancellationToken)
            )
            .WithMessage("A discussion with this title already exists for the package.")
            .OverridePropertyName(nameof(CreateDiscussionCommand.Title));
    }
}
