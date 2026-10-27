using AutoMapper;
using Cicd.Bll.Dtos;
using Cicd.Bll.Mapping;
using Cicd.Bll.Services;
using Cicd.Dal.UnitOfWork;
using Cicd.Domain.Exceptions;
using Cicd.Domain.Models;
using Cicd.Tests.Dal;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Cicd.Tests.Bll;

public sealed class BuildRunServiceTests
{
    private const string ExistingSha256 = "3b1f9c2a7d4e8f0b6c5a9e2d7f1b4c8a0e3d6f9b2c5a8e1d4f7b0c3a6e9d2f5b";

    private static readonly IMapper s_mapper = new MapperConfiguration(
        expression => expression.AddMaps(typeof(BuildRunProfile).Assembly),
        NullLoggerFactory.Instance
    ).CreateMapper();

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task CompletingUnknownRunThrowsNotFound()
    {
        await using UnitOfWork uow = new(TestDatabase.ConnectionString);
        BuildRunService service = new(uow, s_mapper, NullLogger<BuildRunService>.Instance);

        await Assert.ThrowsAsync<NotFoundException>(() =>
            service.CompleteAsync(
                999_999_999,
                NewCompletion("1111111111111111111111111111111111111111111111111111111111111111"),
                Token
            )
        );
    }

    [Fact]
    public async Task FailureInArtifactRepositoryRollsBackTheStatusChange()
    {
        long runId = await CreateRunningRunAsync();
        try
        {
            BuildRun before = await ReadRunAsync(runId);
            int historyBefore = await CountHistoryAsync(runId);

            await using (UnitOfWork uow = new(TestDatabase.ConnectionString))
            {
                BuildRunService service = new(uow, s_mapper, NullLogger<BuildRunService>.Instance);
                await Assert.ThrowsAsync<BusinessConflictException>(() =>
                    service.CompleteAsync(runId, NewCompletion(ExistingSha256), Token)
                );
            }

            BuildRun after = await ReadRunAsync(runId);
            Assert.Equal(BuildRunStatus.Running, after.Status);
            Assert.Null(after.FinishedAt);
            Assert.Equal(before.RowVersion, after.RowVersion);
            Assert.Equal(historyBefore, await CountHistoryAsync(runId));
        }
        finally
        {
            await RemoveRunAsync(runId);
        }
    }

    private static CompleteBuildRunDto NewCompletion(string sha256) => new()
    {
        CompletedBy = "tests",
        Artifacts =
        [
            new ProducedArtifactDto
            {
                SemVer = "9.9.9",
                SizeBytes = 1,
                Sha256 = sha256,
                StorageUri = "https://artifacts.devhub.local/tests",
            },
        ],
    };

    private static async Task<long> CreateRunningRunAsync()
    {
        await using UnitOfWork uow = new(TestDatabase.ConnectionString);
        long runId = await uow.BuildRuns.QueueAsync(
            new BuildRun { PipelineId = 1 },
            new BuildRunDetail
            {
                CommitSha = "fedcba9876543210fedcba9876543210fedcba98",
                CommitMessage = "service rollback probe",
                BranchName = "main",
                TriggeredBy = "tests",
                TriggerKind = "Manual",
            },
            Token
        );
        long? agentId = await uow.Agents.LeaseAsync(runId, "Windows", "tests", Token);
        Assert.NotNull(agentId);
        return runId;
    }

    private static async Task<BuildRun> ReadRunAsync(long runId)
    {
        await using UnitOfWork uow = new(TestDatabase.ConnectionString);
        return await uow.BuildRuns.GetByIdAsync(runId, Token)
            ?? throw new InvalidOperationException("Probe run vanished.");
    }

    private static async Task<int> CountHistoryAsync(long runId)
    {
        await using SqlConnection connection = new(TestDatabase.ConnectionString);
        await connection.OpenAsync(Token);
        const string Sql = "SELECT COUNT(*) FROM cicd.RunStatusHistory WHERE BuildRunId = @BuildRunId;";
        await using SqlCommand command = new(Sql, connection);
        command.Parameters.Add("@BuildRunId", System.Data.SqlDbType.BigInt).Value = runId;
        return (int)(await command.ExecuteScalarAsync(Token) ?? 0);
    }

    private static async Task RemoveRunAsync(long runId)
    {
        await using SqlConnection connection = new(TestDatabase.ConnectionString);
        await connection.OpenAsync(CancellationToken.None);
        await using SqlCommand command = new(
            "UPDATE cicd.Agent SET Status = 'Idle', UpdatedAt = NULL, UpdatedBy = NULL " +
            "WHERE AgentId = (SELECT AgentId FROM cicd.BuildRun WHERE BuildRunId = @BuildRunId); " +
            "DELETE FROM cicd.BuildRun WHERE BuildRunId = @BuildRunId;",
            connection
        );
        command.Parameters.Add("@BuildRunId", System.Data.SqlDbType.BigInt).Value = runId;
        await command.ExecuteNonQueryAsync(CancellationToken.None);
    }
}
