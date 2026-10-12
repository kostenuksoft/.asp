using Cicd.Dal.UnitOfWork;
using Cicd.Domain.Exceptions;
using Cicd.Domain.Models;
using Xunit;

namespace Cicd.Tests.Dal;

public sealed class UnitOfWorkTests
{
    private const string DuplicateSha256 = "3b1f9c2a7d4e8f0b6c5a9e2d7f1b4c8a0e3d6f9b2c5a8e1d4f7b0c3a6e9d2f5b";

    private static CancellationToken Token => TestContext.Current.CancellationToken;

    [Fact]
    public async Task MultiMappingReturnsRunWithDetailAndOrderedSteps()
    {
        await using UnitOfWork uow = new(TestDatabase.ConnectionString);

        BuildRun? run = await uow.BuildRuns.GetWithDetailAndStepsAsync(1, Token);

        Assert.NotNull(run);
        Assert.NotNull(run.Detail);
        Assert.Equal(1, run.Detail.BuildRunId);
        Assert.Equal([1, 2, 3, 4], run.Steps.Select(step => step.StepOrder));
        Assert.Equal(8, run.RowVersion.Length);
    }

    [Fact]
    public async Task TimelineReadsFourResultSetsFromTheProcedure()
    {
        await using UnitOfWork uow = new(TestDatabase.ConnectionString);

        RunTimeline? timeline = await uow.BuildRuns.GetTimelineAsync(1, Token);

        Assert.NotNull(timeline);
        Assert.Equal("devhub-auth CI", timeline.PipelineName);
        Assert.Equal(4, timeline.Steps.Count);
        Assert.Equal(3, timeline.History.Count);
        Assert.Equal(2, timeline.Artifacts.Count);
    }

    [Fact]
    public async Task UnknownRunIsTranslatedToNotFound()
    {
        await using UnitOfWork uow = new(TestDatabase.ConnectionString);

        await Assert.ThrowsAsync<NotFoundException>(() => uow.BuildRuns.GetTimelineAsync(999_999_999, Token));
    }

    [Fact]
    public async Task ApostropheInInputIsStoredAsData()
    {
        await using UnitOfWork uow = new(TestDatabase.ConnectionString);
        await uow.BeginTransactionAsync(cancellationToken: Token);

        long runId = await uow.BuildRuns.QueueAsync(
            new BuildRun { PipelineId = 1 },
            NewDetail("O'Brien", "Fix O'Brien's 'quoted' path"),
            Token);
        BuildRun? stored = await uow.BuildRuns.GetWithDetailAndStepsAsync(runId, Token);

        await uow.RollbackAsync(Token);

        Assert.NotNull(stored?.Detail);
        Assert.Equal("O'Brien", stored.Detail.TriggeredBy);
        Assert.Equal("Fix O'Brien's 'quoted' path", stored.Detail.CommitMessage);
    }

    [Fact]
    public async Task FailureInSecondRepositoryRollsBackTheFirst()
    {
        await using UnitOfWork uow = new(TestDatabase.ConnectionString);
        await uow.BeginTransactionAsync(cancellationToken: Token);

        long runId = await uow.BuildRuns.QueueAsync(new BuildRun { PipelineId = 1 }, NewDetail("tests", "rollback probe"), Token);
        Artifact duplicate = new()
        {
            PackageId = 2,
            PackageKey = "platform-team/devhub-auth",
            PackageName = "devhub-auth",
            PublisherSlug = "platform-team",
            SemVer = "9.9.9",
            SizeBytes = 1,
            Sha256 = DuplicateSha256,
            StorageUri = "https://artifacts.devhub.local/probe",
            CreatedBy = "tests",
        };

        await Assert.ThrowsAsync<BusinessConflictException>(() => uow.Artifacts.AddAsync(duplicate, Token));
        await uow.RollbackAsync(Token);

        Assert.Null(await uow.BuildRuns.GetByIdAsync(runId, Token));
    }

    [Fact]
    public async Task ProcedureErrorAbortsTransactionAndRollbackStillSucceeds()
    {
        await using UnitOfWork uow = new(TestDatabase.ConnectionString);
        await uow.BeginTransactionAsync(cancellationToken: Token);

        long runId = await uow.BuildRuns.QueueAsync(new BuildRun { PipelineId = 1 }, NewDetail("tests", "illegal move"), Token);

        await Assert.ThrowsAsync<BusinessConflictException>(() =>
            uow.BuildRuns.TransitionStatusAsync(runId, BuildRunStatus.Succeeded, null, "tests", null, Token));
        await uow.RollbackAsync(Token);

        Assert.Null(await uow.BuildRuns.GetByIdAsync(runId, Token));
        Assert.NotNull(await uow.Pipelines.GetByIdAsync(1, Token));
    }

    [Fact]
    public async Task AdoNetRepositoryLeasesAnIdleAgentThroughOutputParameter()
    {
        await using UnitOfWork uow = new(TestDatabase.ConnectionString);
        await uow.BeginTransactionAsync(cancellationToken: Token);

        long runId = await uow.BuildRuns.QueueAsync(new BuildRun { PipelineId = 1 }, NewDetail("tests", "lease probe"), Token);
        long? agentId = await uow.Agents.LeaseAsync(runId, "Linux", "tests", Token);
        Agent? agent = agentId is null ? null : await uow.Agents.GetByIdAsync(agentId.Value, Token);
        BuildRun? run = await uow.BuildRuns.GetByIdAsync(runId, Token);

        await uow.RollbackAsync(Token);

        Assert.NotNull(agent);
        Assert.Equal(AgentStatus.Leased, agent.Status);
        Assert.Equal("Linux", agent.OperatingSystem);
        Assert.Equal(BuildRunStatus.Running, run?.Status);
    }

    private static BuildRunDetail NewDetail(string triggeredBy, string message) => new()
    {
        CommitSha = "0123456789abcdef0123456789abcdef01234567",
        CommitMessage = message,
        BranchName = "main",
        TriggeredBy = triggeredBy,
        TriggerKind = "Manual",
    };
}
