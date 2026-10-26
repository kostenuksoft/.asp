using FluentValidation;
using Registry.Bll.Dtos;

namespace Registry.Bll.Validation;

public sealed class RenamePackageDtoValidator : AbstractValidator<RenamePackageDto>
{
    public RenamePackageDtoValidator()
    {
        RuleFor(input => input.Name)
            .Matches("^[A-Za-z0-9._-]+$")
            .WithMessage("Name may contain only letters, digits, dot, underscore and hyphen.");
    }
}
