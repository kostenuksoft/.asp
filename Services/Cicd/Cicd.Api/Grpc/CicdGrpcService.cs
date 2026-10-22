using Cicd.Bll.Dtos;
using Cicd.Bll.Services.Interfaces;
using Cicd.Domain.Exceptions;
using Cicd.Grpc;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;

namespace Cicd.Api.Grpc;

public sealed class CicdGrpcService(IBuildRunService buildRuns) : CicdService.CicdServiceBase
{
    public override async Task<RunTimelineReply> GetRunTimeline(
        GetRunTimelineRequest request,
        ServerCallContext context
    )
    {
        ArgumentNullException.ThrowIfNull(request);
        ArgumentNullException.ThrowIfNull(context);

        if (request.BuildRunId <= 0)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Build run id must be positive."));
        }

        try
        {
            RunTimelineDto timeline = await buildRuns.GetTimelineAsync(request.BuildRunId, context.CancellationToken);
            return ToReply(timeline);
        }
        catch (NotFoundException exception)
        {
            throw new RpcException(new Status(StatusCode.NotFound, exception.Message));
        }
        catch (ValidationException exception)
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, exception.Message));
        }
    }

    private static RunTimelineReply ToReply(RunTimelineDto timeline)
    {
        RunTimelineReply reply = new()
        {
            BuildRunId = timeline.BuildRunId,
            PipelineName = timeline.PipelineName,
            RunNumber = timeline.RunNumber,
            Status = timeline.Status,
            BranchName = timeline.BranchName,
            CommitSha = timeline.CommitSha,
            QueuedAt = ToTimestamp(timeline.QueuedAt),
            FinishedAt = timeline.FinishedAt is DateTime finishedAt ? ToTimestamp(finishedAt) : null,
            Artifacts =
            {
                timeline.Artifacts.Select(artifact => new RunArtifact
                {
                    UsageKind = artifact.UsageKind,
                    PackageId = artifact.PackageId,
                    PackageKey = artifact.PackageKey,
                    SemVer = artifact.SemVer
                })
            }
        };
        if (timeline.DurationSeconds is int durationSeconds)
        {
            reply.DurationSeconds = durationSeconds;
        }

        return reply;
    }

    private static Timestamp ToTimestamp(DateTime utc) => Timestamp.FromDateTime(DateTime.SpecifyKind(utc, DateTimeKind.Utc));
}
