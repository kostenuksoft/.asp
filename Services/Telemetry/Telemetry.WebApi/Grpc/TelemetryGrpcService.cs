using System.Text.Json;
using FluentValidation;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using MediatR;
using Telemetry.Application.Common.Models;
using Telemetry.Application.CrashReports.Queries.GetCrashReportsList;
using Telemetry.Application.Discussions.Queries.GetDiscussionsList;
using Telemetry.Grpc;

namespace Telemetry.WebApi.Grpc;

public sealed class TelemetryGrpcService(ISender sender) : TelemetryService.TelemetryServiceBase
{
    public override async Task<DiscussionsReply> GetDiscussions(
        PackageItemsRequest request,
        ServerCallContext context
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        Validate(request);

        try
        {
            PagedResult<DiscussionSummaryDto> page = await sender.Send(
                new GetDiscussionsListQuery(request.PackageKey, null, null, 1, request.PageSize),
                context.CancellationToken
            );
            return new DiscussionsReply
            {
                TotalCount = page.TotalCount,
                Items =
                {
                    page.Items.Select(discussion => new DiscussionItem
                    {
                        Id = discussion.Id,
                        PackageKey = discussion.PackageKey,
                        Title = discussion.Title,
                        Category = ToName(discussion.Category),
                        State = ToName(discussion.State),
                        CommentCount = discussion.CommentCount,
                        OpenedAt = Timestamp.FromDateTime(DateTime.SpecifyKind(discussion.OpenedAt, DateTimeKind.Utc))
                    })
                }
            };
        }
        catch (ValidationException exception)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
        }
    }

    public override async Task<CrashReportsReply> GetCrashReports(
        PackageItemsRequest request,
        ServerCallContext context
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);
        Validate(request);

        try
        {
            PagedResult<CrashReportSummaryDto> page = await sender.Send(
                new GetCrashReportsListQuery(request.PackageKey, null, null, 1, request.PageSize),
                context.CancellationToken
            );
            return new CrashReportsReply
            {
                TotalCount = page.TotalCount,
                Items = { page.Items.Select(ToItem) }
            };
        }
        catch (ValidationException exception)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
        }
    }

    private static void Validate(PackageItemsRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.PackageKey))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Package key is required."));
        }

        if (request.PageSize <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Page size must be positive."));
        }
    }

    private static CrashReportItem ToItem(CrashReportSummaryDto report)
    {
        CrashReportItem item = new()
        {
            Id = report.Id,
            PackageKey = report.PackageKey,
            Kind = ToName(report.Kind),
            Fingerprint = report.Fingerprint,
            OccurredAt = Timestamp.FromDateTime(DateTime.SpecifyKind(report.OccurredAt, DateTimeKind.Utc))
        };
        if (report.Severity is { } severity)
        {
            item.Severity = ToName(severity);
        }

        return item;
    }

    private static string ToName<TEnum>(TEnum value)
        where TEnum : struct, System.Enum => JsonNamingPolicy.CamelCase.ConvertName(value.ToString());
}
