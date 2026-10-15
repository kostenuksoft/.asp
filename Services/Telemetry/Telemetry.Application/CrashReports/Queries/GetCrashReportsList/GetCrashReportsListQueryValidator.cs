using FluentValidation;

namespace Telemetry.Application.CrashReports.Queries.GetCrashReportsList;

public sealed class GetCrashReportsListQueryValidator : AbstractValidator<GetCrashReportsListQuery>
{
    public GetCrashReportsListQueryValidator()
    {
        RuleFor(query => query.Page).GreaterThanOrEqualTo(1);
        RuleFor(query => query.Kind).IsInEnum();
        RuleFor(query => query.Severity).IsInEnum();
        RuleFor(query => query.PackageKey).MaximumLength(300);
    }
}
