using AutoMapper;
using MediatR;
using MongoDB.Bson;
using Telemetry.Application.CrashReports.Queries.GetCrashReportById;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Enums;
using Telemetry.Domain.Exceptions;
using Telemetry.Domain.Interfaces;
using Telemetry.Domain.ValueObjects;

namespace Telemetry.Application.CrashReports.Commands.CreateCrashReport;

public sealed class CreateCrashReportCommandHandler(
    ICrashReportRepository reports,
    IMapper mapper
)
    : IRequestHandler<CreateCrashReportCommand, CrashReportDto>
{
    private const string CreatedBy = "telemetry-api";

    public async Task<CrashReportDto> Handle(
        CreateCrashReportCommand request,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(request);

        CrashReport report = request.Kind switch
        {
            CrashReportKind.UnhandledException => CrashReport.ForUnhandledException(
                request.PackageVersionId,
                request.VersionKey,
                request.PackageId,
                request.PackageKey,
                request.Fingerprint,
                request.OccurredAt,
                request.Environment,
                request.Severity,
                request.Context,
                CreatedBy,
                new ExceptionInfo(request.Exception!.Type, request.Exception.Message, request.Exception.InnerType),
                [.. request.StackFrames!.Select(frame => new StackFrame(frame.Method, frame.File, frame.Line))]
            ),
            CrashReportKind.PerformanceDegradation => CrashReport.ForPerformanceDegradation(
                request.PackageVersionId,
                request.VersionKey,
                request.PackageId,
                request.PackageKey,
                request.Fingerprint,
                request.OccurredAt,
                request.Environment,
                request.Severity,
                request.Context,
                CreatedBy,
                new PerformanceMetric(
                    request.Metric!.Name,
                    request.Metric.Observed,
                    request.Metric.Threshold,
                    request.Metric.Unit
                )
            ),
            CrashReportKind.HealthCheckFailure => CrashReport.ForHealthCheckFailure(
                request.PackageVersionId,
                request.VersionKey,
                request.PackageId,
                request.PackageKey,
                request.Fingerprint,
                request.OccurredAt,
                request.Environment,
                request.Severity,
                request.Context,
                CreatedBy,
                request.CheckName!
            ),
            _ => throw new DomainException($"Unknown crash report kind {request.Kind}."),
        };

        if (request.DiscussionId is not null)
        {
            report.LinkToDiscussion(ObjectId.Parse(request.DiscussionId));
        }

        await reports.AddAsync(report, cancellationToken);
        return mapper.Map<CrashReportDto>(report);
    }
}
