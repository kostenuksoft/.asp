using FluentValidation;
using MongoDB.Bson;

namespace Telemetry.Application.CrashReports.Queries.GetCrashReportById;

public sealed class GetCrashReportByIdQueryValidator : AbstractValidator<GetCrashReportByIdQuery>
{
    public GetCrashReportByIdQueryValidator()
    {
        RuleFor(query => query.Id)
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("Id must be a valid ObjectId");
    }
}
