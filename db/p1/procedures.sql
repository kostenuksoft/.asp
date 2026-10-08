USE DevHub_Cicd;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

CREATE OR ALTER PROCEDURE cicd.usp_QueueBuildRun
    @PipelineId     BIGINT,
    @CommitSha      CHAR(40),
    @CommitMessage  NVARCHAR(1000),
    @BranchName     NVARCHAR(200),
    @TriggeredBy    NVARCHAR(128),
    @TriggerKind    VARCHAR(20),
    @TriggerPayload NVARCHAR(MAX) = NULL,
    @BuildRunId     BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @IsEnabled BIT;

    SELECT @IsEnabled = IsEnabled
    FROM cicd.Pipeline WITH (UPDLOCK, HOLDLOCK)
    WHERE PipelineId = @PipelineId
      AND IsDeleted = 0;

    IF @IsEnabled IS NULL
        THROW 50001, N'Pipeline does not exist or has been deleted.', 1;

    IF @IsEnabled = 0
        THROW 50002, N'Pipeline is disabled and cannot queue new runs.', 1;

    DECLARE @RunNumber INT =
    (
        SELECT ISNULL(MAX(RunNumber), 0) + 1
        FROM cicd.BuildRun
        WHERE PipelineId = @PipelineId
    );

    INSERT INTO cicd.BuildRun (PipelineId, RunNumber, Status, QueuedAt, CreatedBy)
    VALUES (@PipelineId, @RunNumber, 'Queued', SYSUTCDATETIME(), @TriggeredBy);

    SET @BuildRunId = SCOPE_IDENTITY();

    INSERT INTO cicd.BuildRunDetail (BuildRunId, CommitSha, CommitMessage, BranchName, TriggeredBy, TriggerKind, TriggerPayload, CreatedBy)
    VALUES (@BuildRunId, @CommitSha, @CommitMessage, @BranchName, @TriggeredBy, @TriggerKind, @TriggerPayload, @TriggeredBy);

    INSERT INTO cicd.RunStatusHistory (BuildRunId, FromStatus, ToStatus, Reason, CreatedBy)
    VALUES (@BuildRunId, NULL, 'Queued', CONCAT(N'Queued by ', @TriggerKind, N' trigger'), @TriggeredBy);

    COMMIT TRANSACTION;

    SELECT @BuildRunId AS BuildRunId, @RunNumber AS RunNumber;
END;
GO

CREATE OR ALTER PROCEDURE cicd.usp_GetRunTimeline
    @BuildRunId BIGINT
AS
BEGIN
    SET NOCOUNT ON;

    IF NOT EXISTS (SELECT 1 FROM cicd.BuildRun WHERE BuildRunId = @BuildRunId AND IsDeleted = 0)
        THROW 50040, N'Build run does not exist or has been deleted.', 1;

    SELECT
        r.BuildRunId,
        r.PipelineId,
        p.Name AS PipelineName,
        p.RepositoryUrl,
        r.RunNumber,
        r.Status,
        r.QueuedAt,
        r.StartedAt,
        r.FinishedAt,
        r.DurationSeconds,
        r.AgentId,
        a.Hostname AS AgentHostname,
        d.CommitSha,
        d.CommitMessage,
        d.BranchName,
        d.TriggeredBy,
        d.TriggerKind,
        r.RowVersion
    FROM cicd.BuildRun AS r
    INNER JOIN cicd.Pipeline AS p ON p.PipelineId = r.PipelineId
    INNER JOIN cicd.BuildRunDetail AS d ON d.BuildRunId = r.BuildRunId
    LEFT JOIN cicd.Agent AS a ON a.AgentId = r.AgentId
    WHERE r.BuildRunId = @BuildRunId;

    SELECT
        s.BuildStepId,
        s.StepOrder,
        s.Name,
        s.Status,
        s.StartedAt,
        s.FinishedAt,
        s.ExitCode
    FROM cicd.BuildStep AS s
    WHERE s.BuildRunId = @BuildRunId
      AND s.IsDeleted = 0
    ORDER BY s.StepOrder;

    SELECT
        h.RunStatusHistoryId,
        h.FromStatus,
        h.ToStatus,
        h.Reason,
        h.CreatedAt,
        h.CreatedBy
    FROM cicd.RunStatusHistory AS h
    WHERE h.BuildRunId = @BuildRunId
    ORDER BY h.CreatedAt, h.RunStatusHistoryId;

    SELECT
        u.UsageKind,
        a.ArtifactId,
        a.PackageId,
        a.PackageKey,
        a.PackageName,
        a.SemVer,
        a.SizeBytes,
        a.StorageUri
    FROM cicd.ArtifactUsage AS u
    INNER JOIN cicd.Artifact AS a ON a.ArtifactId = u.ArtifactId
    WHERE u.BuildRunId = @BuildRunId
    ORDER BY u.UsageKind, a.PackageKey, a.SemVer;
END;
GO

CREATE OR ALTER PROCEDURE cicd.usp_TransitionRunStatus
    @BuildRunId         BIGINT,
    @ToStatus           VARCHAR(20),
    @Reason             NVARCHAR(400) = NULL,
    @ChangedBy          NVARCHAR(128),
    @ExpectedRowVersion BINARY(8) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @FromStatus VARCHAR(20);
    DECLARE @ActualRowVersion BINARY(8);
    DECLARE @AgentId BIGINT;

    SELECT @FromStatus = Status,
           @ActualRowVersion = RowVersion,
           @AgentId = AgentId
    FROM cicd.BuildRun WITH (UPDLOCK, HOLDLOCK)
    WHERE BuildRunId = @BuildRunId
      AND IsDeleted = 0;

    IF @FromStatus IS NULL
        THROW 50020, N'Build run does not exist or has been deleted.', 1;

    IF @ExpectedRowVersion IS NOT NULL AND @ExpectedRowVersion <> @ActualRowVersion
        THROW 50021, N'Build run was modified by another process.', 1;

    IF NOT
    (
        (@FromStatus = 'Queued'  AND @ToStatus = 'Cancelled')
        OR (@FromStatus = 'Running' AND @ToStatus IN ('Succeeded', 'Failed', 'Cancelled'))
    )
        THROW 50022, N'Illegal build run status transition.', 1;

    UPDATE cicd.BuildRun
    SET Status = @ToStatus,
        FinishedAt = CASE WHEN StartedAt IS NOT NULL THEN SYSUTCDATETIME() END,
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @ChangedBy
    WHERE BuildRunId = @BuildRunId;

    INSERT INTO cicd.RunStatusHistory (BuildRunId, FromStatus, ToStatus, Reason, CreatedBy)
    VALUES (@BuildRunId, @FromStatus, @ToStatus, @Reason, @ChangedBy);

    IF @AgentId IS NOT NULL
        UPDATE cicd.Agent
        SET Status = 'Idle',
            UpdatedAt = SYSUTCDATETIME(),
            UpdatedBy = @ChangedBy
        WHERE AgentId = @AgentId
          AND Status = 'Leased';

    COMMIT TRANSACTION;

    SELECT BuildRunId, Status, StartedAt, FinishedAt, DurationSeconds, RowVersion
    FROM cicd.BuildRun
    WHERE BuildRunId = @BuildRunId;
END;
GO

CREATE OR ALTER PROCEDURE cicd.usp_DeleteBuildRun
    @BuildRunId         BIGINT,
    @DeletedBy          NVARCHAR(128),
    @ExpectedRowVersion BINARY(8) = NULL
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @Status VARCHAR(20);
    DECLARE @ActualRowVersion BINARY(8);

    SELECT @Status = Status,
           @ActualRowVersion = RowVersion
    FROM cicd.BuildRun WITH (UPDLOCK, HOLDLOCK)
    WHERE BuildRunId = @BuildRunId
      AND IsDeleted = 0;

    IF @Status IS NULL
        THROW 50050, N'Build run does not exist or has been deleted.', 1;

    IF @ExpectedRowVersion IS NOT NULL AND @ExpectedRowVersion <> @ActualRowVersion
        THROW 50051, N'Build run was modified by another process.', 1;

    IF @Status IN ('Queued', 'Running')
        THROW 50052, N'An active build run cannot be deleted; cancel it first.', 1;

    UPDATE cicd.BuildStep
    SET IsDeleted = 1,
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @DeletedBy
    WHERE BuildRunId = @BuildRunId
      AND IsDeleted = 0;

    UPDATE cicd.BuildRunDetail
    SET IsDeleted = 1,
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @DeletedBy
    WHERE BuildRunId = @BuildRunId;

    UPDATE cicd.BuildRun
    SET IsDeleted = 1,
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @DeletedBy
    WHERE BuildRunId = @BuildRunId;

    COMMIT TRANSACTION;
END;
GO

CREATE OR ALTER PROCEDURE cicd.usp_LeaseAgent
    @BuildRunId      BIGINT,
    @OperatingSystem NVARCHAR(50),
    @LeasedBy        NVARCHAR(128),
    @AgentId         BIGINT OUTPUT
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    SET @AgentId = NULL;

    BEGIN TRANSACTION;

    DECLARE @CurrentStatus VARCHAR(20);

    SELECT @CurrentStatus = Status
    FROM cicd.BuildRun WITH (UPDLOCK, HOLDLOCK)
    WHERE BuildRunId = @BuildRunId
      AND IsDeleted = 0;

    IF @CurrentStatus IS NULL
        THROW 50010, N'Build run does not exist or has been deleted.', 1;

    IF @CurrentStatus <> 'Queued'
        THROW 50011, N'Only a queued build run can lease an agent.', 1;

    UPDATE TOP (1) a
    SET a.Status = 'Leased',
        a.UpdatedAt = SYSUTCDATETIME(),
        a.UpdatedBy = @LeasedBy,
        @AgentId = a.AgentId
    FROM cicd.Agent AS a WITH (UPDLOCK, READPAST)
    WHERE a.Status = 'Idle'
      AND a.OperatingSystem = @OperatingSystem
      AND a.IsDeleted = 0;

    IF @AgentId IS NOT NULL
    BEGIN
        UPDATE cicd.BuildRun
        SET AgentId = @AgentId,
            Status = 'Running',
            StartedAt = SYSUTCDATETIME(),
            UpdatedAt = SYSUTCDATETIME(),
            UpdatedBy = @LeasedBy
        WHERE BuildRunId = @BuildRunId;

        INSERT INTO cicd.RunStatusHistory (BuildRunId, FromStatus, ToStatus, Reason, CreatedBy)
        SELECT @BuildRunId, 'Queued', 'Running', CONCAT(N'Leased agent ', a.Hostname), @LeasedBy
        FROM cicd.Agent AS a
        WHERE a.AgentId = @AgentId;
    END;

    COMMIT TRANSACTION;

    SELECT @AgentId AS AgentId;
END;
GO

CREATE OR ALTER PROCEDURE cicd.usp_CompleteBuildRun
    @BuildRunId        BIGINT,
    @CompletedBy       NVARCHAR(128),
    @ProducedArtifacts cicd.ArtifactReference READONLY
AS
BEGIN
    SET NOCOUNT ON;
    SET XACT_ABORT ON;

    BEGIN TRANSACTION;

    DECLARE @FromStatus VARCHAR(20);
    DECLARE @AgentId BIGINT;

    SELECT @FromStatus = Status,
           @AgentId = AgentId
    FROM cicd.BuildRun WITH (UPDLOCK, HOLDLOCK)
    WHERE BuildRunId = @BuildRunId
      AND IsDeleted = 0;

    IF @FromStatus IS NULL
        THROW 50030, N'Build run does not exist or has been deleted.', 1;

    IF @FromStatus <> 'Running'
        THROW 50031, N'Only a running build run can be completed.', 1;

    UPDATE cicd.BuildStep
    SET Status = 'Skipped',
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @CompletedBy
    WHERE BuildRunId = @BuildRunId
      AND Status = 'Pending';

    DECLARE @FailedSteps INT =
    (
        SELECT COUNT(*)
        FROM cicd.BuildStep
        WHERE BuildRunId = @BuildRunId
          AND Status = 'Failed'
    );

    DECLARE @ToStatus VARCHAR(20) = CASE WHEN @FailedSteps > 0 THEN 'Failed' ELSE 'Succeeded' END;

    INSERT INTO cicd.Artifact (PackageId, PackageKey, PackageName, PublisherSlug, SemVer, SizeBytes, Sha256, StorageUri, CreatedBy)
    SELECT p.PackageId, p.PackageKey, p.PackageName, p.PublisherSlug, p.SemVer, p.SizeBytes, p.Sha256, p.StorageUri, @CompletedBy
    FROM @ProducedArtifacts AS p
    WHERE NOT EXISTS (SELECT 1 FROM cicd.Artifact AS a WHERE a.Sha256 = p.Sha256);

    INSERT INTO cicd.ArtifactUsage (BuildRunId, ArtifactId, UsageKind, CreatedBy)
    SELECT @BuildRunId, a.ArtifactId, 'Produced', @CompletedBy
    FROM cicd.Artifact AS a
    INNER JOIN @ProducedArtifacts AS p ON p.Sha256 = a.Sha256
    WHERE NOT EXISTS
    (
        SELECT 1
        FROM cicd.ArtifactUsage AS u
        WHERE u.BuildRunId = @BuildRunId
          AND u.ArtifactId = a.ArtifactId
          AND u.UsageKind = 'Produced'
    );

    UPDATE cicd.BuildRun
    SET Status = @ToStatus,
        FinishedAt = SYSUTCDATETIME(),
        UpdatedAt = SYSUTCDATETIME(),
        UpdatedBy = @CompletedBy
    WHERE BuildRunId = @BuildRunId;

    INSERT INTO cicd.RunStatusHistory (BuildRunId, FromStatus, ToStatus, Reason, CreatedBy)
    VALUES (@BuildRunId, @FromStatus, @ToStatus, CONCAT(N'Completed with ', @FailedSteps, N' failed step(s)'), @CompletedBy);

    IF @AgentId IS NOT NULL
        UPDATE cicd.Agent
        SET Status = 'Idle',
            UpdatedAt = SYSUTCDATETIME(),
            UpdatedBy = @CompletedBy
        WHERE AgentId = @AgentId
          AND Status = 'Leased';

    COMMIT TRANSACTION;

    SELECT BuildRunId, Status, FinishedAt, DurationSeconds
    FROM cicd.BuildRun
    WHERE BuildRunId = @BuildRunId;
END;
GO
