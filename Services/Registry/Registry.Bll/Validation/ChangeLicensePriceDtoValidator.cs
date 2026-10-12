using FluentValidation;
using Registry.Bll.Dtos;

namespace Registry.Bll.Validation;

public sealed class ChangeLicensePriceDtoValidator : AbstractValidator<ChangeLicensePriceDto>
{
    public ChangeLicensePriceDtoValidator()
    {
        RuleFor(input => input.LicensePrice)
            .GreaterThanOrEqualTo(0)
            .PrecisionScale(18, 2, true);
    }
}
