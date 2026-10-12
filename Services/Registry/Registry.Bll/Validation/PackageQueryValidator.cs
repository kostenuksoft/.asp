using FluentValidation;
using Registry.Bll.Dtos;

namespace Registry.Bll.Validation;

public sealed class PackageQueryValidator : AbstractValidator<PackageQuery>
{
    private static readonly string[] s_sortFields = ["name", "price", "downloads"];
    private static readonly string[] s_sortDirections = ["asc", "desc"];

    public PackageQueryValidator()
    {
        RuleFor(query => query.SortBy)
            .Must(sortBy => s_sortFields.Contains(sortBy, StringComparer.OrdinalIgnoreCase))
            .WithMessage($"SortBy must be one of: {string.Join(", ", s_sortFields)}.");

        RuleFor(query => query.SortDir)
            .Must(sortDir => s_sortDirections.Contains(sortDir, StringComparer.OrdinalIgnoreCase))
            .WithMessage("SortDir must be asc or desc.");

        RuleFor(query => query.MinPrice)
            .GreaterThanOrEqualTo(0)
            .When(query => query.MinPrice is not null);

        RuleFor(query => query.MaxPrice)
            .GreaterThanOrEqualTo(query => query.MinPrice!.Value)
            .When(query => query.MinPrice is not null && query.MaxPrice is not null)
            .WithMessage("MaxPrice cannot be lower than MinPrice.");
    }
}
