using FluentValidation;
using MongoDB.Bson;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Enums;
using Telemetry.Domain.Interfaces;
using Telemetry.Domain.ValueObjects;

namespace Telemetry.Application.CrashReports.Commands.CreateCrashReport;

public sealed class CreateCrashReportCommandValidator : AbstractValidator<CreateCrashReportCommand>
{
    public CreateCrashReportCommandValidator(IDiscussionRepository discussions)
    {
        RuleFor(command => command.Kind).IsInEnum();
        RuleFor(command => command.PackageVersionId).GreaterThan(0);
        RuleFor(command => command.PackageId).GreaterThan(0);
        RuleFor(command => command.PackageKey).NotEmpty().MaximumLength(300);
        RuleFor(command => command.VersionKey)
            .NotEmpty()
            .Must((command, versionKey) => versionKey.StartsWith($"{command.PackageKey}@", StringComparison.Ordinal))
            .WithMessage("VersionKey must be '<packageKey>@<semver>'.");
        RuleFor(command => command.Fingerprint).NotEmpty().MaximumLength(CrashReport.MaxFingerprintLength);
        RuleFor(command => command.Environment).IsInEnum();
        RuleFor(command => command.Severity).IsInEnum();

        When(command => command.Kind == CrashReportKind.UnhandledException, () =>
        {
            RuleFor(command => command.Exception)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .ChildRules(exception =>
                {
                    exception.RuleFor(info => info!.Type).NotEmpty().MaximumLength(ExceptionInfo.MaxTypeLength);
                    exception.RuleFor(info => info!.Message).NotEmpty();
                });
            RuleFor(command => command.StackFrames)
                .Cascade(CascadeMode.Stop)
                .NotEmpty()
                .Must(frames => frames!.Count <= CrashReport.MaxStackFrames)
                .WithMessage($"At most {CrashReport.MaxStackFrames} stack frames.");
            RuleForEach(command => command.StackFrames).ChildRules(frame =>
            {
                frame.RuleFor(item => item.Method).NotEmpty();
                frame.RuleFor(item => item.Line).GreaterThanOrEqualTo(0);
            });
        });

        When(command => command.Kind == CrashReportKind.PerformanceDegradation, () =>
            RuleFor(command => command.Metric)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .ChildRules(metric =>
                {
                    metric.RuleFor(item => item!.Name).NotEmpty();
                    metric.RuleFor(item => item!.Threshold).GreaterThan(0);
                    metric.RuleFor(item => item!.Observed).GreaterThan(item => item!.Threshold);
                })
        );

        When(command => command.Kind == CrashReportKind.HealthCheckFailure, () =>
            RuleFor(command => command.CheckName).NotEmpty()
        );

        RuleFor(command => command.DiscussionId)
            .Cascade(CascadeMode.Stop)
            .Must(id => ObjectId.TryParse(id, out _))
            .WithMessage("DiscussionId must be a valid ObjectId")
            .MustAsync(async (id, cancellationToken) =>
                await discussions.ExistsAsync(ObjectId.Parse(id), cancellationToken)
            )
            .WithMessage("The discussion does not exist.")
            .When(command => command.DiscussionId is not null);
    }
}
