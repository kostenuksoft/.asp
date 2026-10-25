using FluentValidation;

namespace Telemetry.Application.PackageCopies.Commands.SyncPackageName;

public sealed class SyncPackageNameCommandValidator : AbstractValidator<SyncPackageNameCommand>
{
    public SyncPackageNameCommandValidator()
    {
        RuleFor(command => command.EventId).NotEmpty();
        RuleFor(command => command.PackageId).GreaterThan(0);
        RuleFor(command => command.PublisherSlug).NotEmpty();
        RuleFor(command => command.PackageName).NotEmpty();
        RuleFor(command => command.PackageKey)
            .Must((command, packageKey) => packageKey == $"{command.PublisherSlug}/{command.PackageName}")
            .WithMessage("PackageKey must be '<publisher>/<name>' of the same package");
    }
}
