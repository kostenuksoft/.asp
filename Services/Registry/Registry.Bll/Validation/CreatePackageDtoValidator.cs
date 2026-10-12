using FluentValidation;
using Registry.Bll.Dtos;

namespace Registry.Bll.Validation;

public sealed class CreatePackageDtoValidator : AbstractValidator<CreatePackageDto>
{
    public CreatePackageDtoValidator()
    {
        RuleFor(input => input.Name)
            .Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("Name may contain only letters, digits, dot, underscore and hyphen.");

        RuleFor(input => input.ProjectUrl)
            .Must(url => Uri.TryCreate(url, UriKind.Absolute, out Uri? uri) && uri.Scheme == Uri.UriSchemeHttps)
            .When(input => input.ProjectUrl is not null)
            .WithMessage("ProjectUrl must be an absolute https:// address.");

        RuleFor(input => input.LicensePrice)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, true);
    }
}
