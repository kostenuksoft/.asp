using System.Data;
using Cicd.Dal.Repositories.Interfaces;
using Cicd.Domain.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Cicd.Dal.Repositories;

public sealed class BuildRunRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
    : BaseDapperRepository<BuildRun>(connection, currentTransaction, "cicd.BuildRun", "BuildRunId"), IBuildRunRepository
{
    private const string RunWithDetailAndStepsSql =
        "SELECT r.BuildRunId, r.PipelineId, r.AgentId, r.RunNumber, r.Status, r.QueuedAt, r.StartedAt, r.FinishedAt, " +
        "r.DurationSeconds, r.CreatedAt, r.CreatedBy, r.UpdatedAt, r.UpdatedBy, r.IsDeleted, r.RowVersion, " +
        "d.BuildRunId, d.CommitSha, d.CommitMessage, d.BranchName, d.TriggeredBy, d.TriggerKind, d.TriggerPayload, " +
        "s.BuildStepId, s.BuildRunId, s.StepOrder, s.Name, s.Status, s.StartedAt, s.FinishedAt, s.ExitCode " +
        "FROM cicd.BuildRun AS r " +
        "INNER JOIN cicd.BuildRunDetail AS d ON d.BuildRunId = r.BuildRunId " +
        "LEFT JOIN cicd.BuildStep AS s ON s.BuildRunId = r.BuildRunId AND s.IsDeleted = 0 " +
        "WHERE r.BuildRunId = @BuildRunId AND r.IsDeleted = 0 " +
        "ORDER BY s.StepOrder;";

    protected override string InsertSql =>
        "INSERT INTO cicd.BuildRun (PipelineId, RunNumber, Status, CreatedBy) " +
        "OUTPUT INSERTED.BuildRunId " +
        "VALUES (@PipelineId, @RunNumber, @Status, @CreatedBy);";

    public async Task<BuildRun?> GetWithDetailAndStepsAsync(
        long buildRunId,
        CancellationToken cancellationToken = default
    )
    {
        BuildRun? result = null;

        await Connection.QueryAsync<BuildRun, BuildRunDetail, BuildStep?, BuildRun>(
            Command(RunWithDetailAndStepsSql, new { BuildRunId = buildRunId }, cancellationToken),
            (run, detail, step) =>
            {
                result ??= run;
                result.Detail ??= detail;
                if (step is not null)
                {
                    result.Steps.Add(step);
                }

                return result;
            },
            splitOn: "BuildRunId,BuildStepId"
        );

        return result;
    }

    public async Task<IReadOnlyList<BuildRun>> GetByPipelineAsync(
        long pipelineId,
        CancellationToken cancellationToken = default
    )
    {
        IEnumerable<BuildRun> rows = await Connection.QueryAsync<BuildRun>(Command(
            "SELECT * FROM cicd.BuildRun WHERE PipelineId = @PipelineId AND IsDeleted = 0 ORDER BY QueuedAt DESC;",
            new { PipelineId = pipelineId },
            cancellationToken
        ));
        return rows.AsList();
    }

    public Task<RunTimeline?> GetTimelineAsync(
        long buildRunId,
        CancellationToken cancellationToken = default
    ) => TranslateErrorsAsync(async () =>
    {
        using SqlMapper.GridReader grid = await Connection.QueryMultipleAsync(
            Procedure("cicd.usp_GetRunTimeline", new { BuildRunId = buildRunId }, cancellationToken)
        );

        RunTimeline? timeline = await grid.ReadSingleOrDefaultAsync<RunTimeline>();
        if (timeline is null)
        {
            return null;
        }

        timeline.Steps = (await grid.ReadAsync<BuildStep>()).AsList();
        timeline.History = (await grid.ReadAsync<RunStatusChange>()).AsList();
        timeline.Artifacts = (await grid.ReadAsync<RunArtifact>()).AsList();
        return timeline;
    });

    public Task<long> QueueAsync(
        BuildRun run,
        BuildRunDetail detail,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(run);
        ArgumentNullException.ThrowIfNull(detail);

        DynamicParameters parameters = new();
        parameters.Add("@PipelineId", run.PipelineId, DbType.Int64);
        parameters.Add("@CommitSha", detail.CommitSha, DbType.AnsiStringFixedLength, size: 40);
        parameters.Add("@CommitMessage", detail.CommitMessage, DbType.String, size: 1000);
        parameters.Add("@BranchName", detail.BranchName, DbType.String, size: 200);
        parameters.Add("@TriggeredBy", detail.TriggeredBy, DbType.String, size: 128);
        parameters.Add("@TriggerKind", detail.TriggerKind, DbType.AnsiString, size: 20);
        parameters.Add("@TriggerPayload", detail.TriggerPayload, DbType.String);
        parameters.Add("@BuildRunId", dbType: DbType.Int64, direction: ParameterDirection.Output);

        return TranslateErrorsAsync(async () =>
        {
            await Connection.ExecuteAsync(Procedure("cicd.usp_QueueBuildRun", parameters, cancellationToken));
            return parameters.Get<long>("@BuildRunId");
        });
    }

    public Task TransitionStatusAsync(
        long buildRunId,
        string toStatus,
        string? reason,
        string changedBy,
        byte[]? expectedRowVersion,
        CancellationToken cancellationToken = default
    )
    {
        DynamicParameters parameters = new();
        parameters.Add("@BuildRunId", buildRunId, DbType.Int64);
        parameters.Add("@ToStatus", toStatus, DbType.AnsiString, size: 20);
        parameters.Add("@Reason", reason, DbType.String, size: 400);
        parameters.Add("@ChangedBy", changedBy, DbType.String, size: 128);
        parameters.Add("@ExpectedRowVersion", expectedRowVersion, DbType.Binary, size: 8);

        return TranslateErrorsAsync(() => Connection.ExecuteAsync(
            Procedure("cicd.usp_TransitionRunStatus", parameters, cancellationToken)
        ));
    }

    public override Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default
    ) => TranslateErrorsAsync(async () =>
    {
        await Connection.ExecuteAsync(Procedure(
            "cicd.usp_DeleteBuildRun",
            new { BuildRunId = id, DeletedBy = "api" },
            cancellationToken
        ));
        return true;
    });
}
