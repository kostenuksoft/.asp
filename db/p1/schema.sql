IF DB_ID(N'DevHub_Cicd') IS NULL
BEGIN
    CREATE DATABASE DevHub_Cicd;
END;
GO

USE DevHub_Cicd;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF SCHEMA_ID(N'cicd') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA cicd AUTHORIZATION dbo;');
END;
GO

IF TYPE_ID(N'cicd.ArtifactReference') IS NULL
BEGIN
    CREATE TYPE cicd.ArtifactReference AS TABLE
    (
        PackageId       BIGINT          NOT NULL,
        PackageKey      NVARCHAR(300)   NOT NULL,
        PackageName     NVARCHAR(200)   NOT NULL,
        PublisherSlug   NVARCHAR(100)   NOT NULL,
        SemVer          NVARCHAR(50)    NOT NULL,
        SizeBytes       BIGINT          NOT NULL,
        Sha256          CHAR(64)        NOT NULL,
        StorageUri      NVARCHAR(500)   NOT NULL,
        PRIMARY KEY (Sha256)
    );
END;
GO

IF OBJECT_ID(N'cicd.Pipeline', N'U') IS NULL
BEGIN
    CREATE TABLE cicd.Pipeline
    (
        PipelineId      BIGINT          IDENTITY(1, 1) NOT NULL,
        Name            NVARCHAR(200)   NOT NULL,
        RepositoryUrl   NVARCHAR(400)   NOT NULL,
        DefaultBranch   NVARCHAR(200)   NOT NULL CONSTRAINT DF_Pipeline_DefaultBranch DEFAULT (N'main'),
        PackageId       BIGINT          NULL,
        PackageKey      NVARCHAR(300)   NULL,
        IsEnabled       BIT             NOT NULL CONSTRAINT DF_Pipeline_IsEnabled DEFAULT (1),
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_Pipeline_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_Pipeline_CreatedBy DEFAULT (SUSER_SNAME()),
        UpdatedAt       DATETIME2(3)    NULL,
        UpdatedBy       NVARCHAR(128)   NULL,
        IsDeleted       BIT             NOT NULL CONSTRAINT DF_Pipeline_IsDeleted DEFAULT (0),
        RowVersion      ROWVERSION      NOT NULL,

        CONSTRAINT PK_Pipeline PRIMARY KEY CLUSTERED (PipelineId),
        CONSTRAINT UQ_Pipeline_RepositoryUrl_Name UNIQUE (RepositoryUrl, Name),
        CONSTRAINT CK_Pipeline_Name_NotBlank CHECK (LEN(LTRIM(RTRIM(Name))) > 0),
        CONSTRAINT CK_Pipeline_RepositoryUrl_Scheme CHECK (RepositoryUrl LIKE N'http://%' OR RepositoryUrl LIKE N'https://%' OR RepositoryUrl LIKE N'git@%'),
        CONSTRAINT CK_Pipeline_PackageKey_Shape CHECK (PackageKey IS NULL OR (PackageKey LIKE N'%_/_%' AND PackageKey NOT LIKE N'%/%/%')),
        CONSTRAINT CK_Pipeline_PackageId_Positive CHECK (PackageId IS NULL OR PackageId > 0),
        CONSTRAINT CK_Pipeline_PackageId_WithKey CHECK ((PackageId IS NULL AND PackageKey IS NULL) OR (PackageId IS NOT NULL AND PackageKey IS NOT NULL))
    );
END;
GO

IF OBJECT_ID(N'cicd.Package', N'U') IS NULL
BEGIN
    CREATE TABLE cicd.Package
    (
        PackageId       BIGINT          NOT NULL,
        PublisherSlug   NVARCHAR(100)   NOT NULL,
        PackageName     NVARCHAR(200)   NOT NULL,
        PackageKey      NVARCHAR(300)   NOT NULL,
        LicensePrice    DECIMAL(18, 2)  NOT NULL,
        IsDeleted       BIT             NOT NULL CONSTRAINT DF_Package_IsDeleted DEFAULT (0),
        SourceChangedAt DATETIME2(3)    NULL,
        SyncedAt        DATETIME2(3)    NOT NULL CONSTRAINT DF_Package_SyncedAt DEFAULT (SYSUTCDATETIME()),
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_Package_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_Package_CreatedBy DEFAULT (SUSER_SNAME()),

        CONSTRAINT PK_Package PRIMARY KEY CLUSTERED (PackageId),
        CONSTRAINT CK_Package_PackageId_Positive CHECK (PackageId > 0),
        CONSTRAINT CK_Package_LicensePrice_NonNegative CHECK (LicensePrice >= 0),
        CONSTRAINT CK_Package_PackageKey_MatchesCopy CHECK (PackageKey = CONCAT(PublisherSlug, N'/', PackageName))
    );
END;
GO

IF OBJECT_ID(N'cicd.ProcessedEvent', N'U') IS NULL
BEGIN
    CREATE TABLE cicd.ProcessedEvent
    (
        EventId         UNIQUEIDENTIFIER NOT NULL,
        EventType       NVARCHAR(200)   NOT NULL,
        ProcessedAt     DATETIME2(3)    NOT NULL CONSTRAINT DF_ProcessedEvent_ProcessedAt DEFAULT (SYSUTCDATETIME()),

        CONSTRAINT PK_ProcessedEvent PRIMARY KEY CLUSTERED (EventId)
    );
END;
GO

IF OBJECT_ID(N'cicd.Agent', N'U') IS NULL
BEGIN
    CREATE TABLE cicd.Agent
    (
        AgentId         BIGINT          IDENTITY(1, 1) NOT NULL,
        Hostname        NVARCHAR(200)   NOT NULL,
        OperatingSystem NVARCHAR(50)    NOT NULL,
        Status          VARCHAR(20)     NOT NULL CONSTRAINT DF_Agent_Status DEFAULT ('Idle'),
        LastHeartbeatAt DATETIME2(3)    NULL,
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_Agent_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_Agent_CreatedBy DEFAULT (SUSER_SNAME()),
        UpdatedAt       DATETIME2(3)    NULL,
        UpdatedBy       NVARCHAR(128)   NULL,
        IsDeleted       BIT             NOT NULL CONSTRAINT DF_Agent_IsDeleted DEFAULT (0),
        RowVersion      ROWVERSION      NOT NULL,

        CONSTRAINT PK_Agent PRIMARY KEY CLUSTERED (AgentId),
        CONSTRAINT UQ_Agent_Hostname UNIQUE (Hostname),
        CONSTRAINT CK_Agent_Status CHECK (Status IN ('Idle', 'Leased', 'Offline', 'Draining')),
        CONSTRAINT CK_Agent_OperatingSystem CHECK (OperatingSystem IN (N'Windows', N'Linux', N'macOS'))
    );
END;
GO

IF OBJECT_ID(N'cicd.BuildRun', N'U') IS NULL
BEGIN
    CREATE TABLE cicd.BuildRun
    (
        BuildRunId      BIGINT          IDENTITY(1, 1) NOT NULL,
        PipelineId      BIGINT          NOT NULL,
        AgentId         BIGINT          NULL,
        RunNumber       INT             NOT NULL,
        Status          VARCHAR(20)     NOT NULL CONSTRAINT DF_BuildRun_Status DEFAULT ('Queued'),
        QueuedAt        DATETIME2(3)    NOT NULL CONSTRAINT DF_BuildRun_QueuedAt DEFAULT (SYSUTCDATETIME()),
        StartedAt       DATETIME2(3)    NULL,
        FinishedAt      DATETIME2(3)    NULL,
        DurationSeconds AS (CASE WHEN StartedAt IS NOT NULL AND FinishedAt IS NOT NULL THEN DATEDIFF(SECOND, StartedAt, FinishedAt) END),
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_BuildRun_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_BuildRun_CreatedBy DEFAULT (SUSER_SNAME()),
        UpdatedAt       DATETIME2(3)    NULL,
        UpdatedBy       NVARCHAR(128)   NULL,
        IsDeleted       BIT             NOT NULL CONSTRAINT DF_BuildRun_IsDeleted DEFAULT (0),
        RowVersion      ROWVERSION      NOT NULL,

        CONSTRAINT PK_BuildRun PRIMARY KEY CLUSTERED (BuildRunId),
        CONSTRAINT FK_BuildRun_Pipeline FOREIGN KEY (PipelineId) REFERENCES cicd.Pipeline (PipelineId),
        CONSTRAINT FK_BuildRun_Agent FOREIGN KEY (AgentId) REFERENCES cicd.Agent (AgentId),
        CONSTRAINT UQ_BuildRun_Pipeline_RunNumber UNIQUE (PipelineId, RunNumber),
        CONSTRAINT CK_BuildRun_Status CHECK (Status IN ('Queued', 'Running', 'Succeeded', 'Failed', 'Cancelled')),
        CONSTRAINT CK_BuildRun_RunNumber_Positive CHECK (RunNumber > 0),
        CONSTRAINT CK_BuildRun_Chronology CHECK (StartedAt IS NULL OR StartedAt >= QueuedAt),
        CONSTRAINT CK_BuildRun_FinishRequiresStart CHECK (FinishedAt IS NULL OR (StartedAt IS NOT NULL AND FinishedAt >= StartedAt)),
        CONSTRAINT CK_BuildRun_TerminalHasFinish CHECK (Status NOT IN ('Succeeded', 'Failed') OR FinishedAt IS NOT NULL),
        CONSTRAINT CK_BuildRun_RunningHasAgent CHECK (Status <> 'Running' OR AgentId IS NOT NULL)
    );
END;
GO

IF OBJECT_ID(N'cicd.BuildRunDetail', N'U') IS NULL
BEGIN
    CREATE TABLE cicd.BuildRunDetail
    (
        BuildRunId      BIGINT          NOT NULL,
        CommitSha       CHAR(40)        NOT NULL,
        CommitMessage   NVARCHAR(1000)  NOT NULL,
        BranchName      NVARCHAR(200)   NOT NULL,
        TriggeredBy     NVARCHAR(128)   NOT NULL,
        TriggerKind     VARCHAR(20)     NOT NULL,
        TriggerPayload  NVARCHAR(MAX)   NULL,
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_BuildRunDetail_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_BuildRunDetail_CreatedBy DEFAULT (SUSER_SNAME()),
        UpdatedAt       DATETIME2(3)    NULL,
        UpdatedBy       NVARCHAR(128)   NULL,
        IsDeleted       BIT             NOT NULL CONSTRAINT DF_BuildRunDetail_IsDeleted DEFAULT (0),

        CONSTRAINT PK_BuildRunDetail PRIMARY KEY CLUSTERED (BuildRunId),
        CONSTRAINT FK_BuildRunDetail_BuildRun FOREIGN KEY (BuildRunId) REFERENCES cicd.BuildRun (BuildRunId) ON DELETE CASCADE,
        CONSTRAINT CK_BuildRunDetail_CommitSha CHECK (LEN(CommitSha) = 40 AND CommitSha NOT LIKE '%[^0-9a-f]%'),
        CONSTRAINT CK_BuildRunDetail_TriggerKind CHECK (TriggerKind IN ('Push', 'PullRequest', 'Manual', 'Schedule', 'Api')),
        CONSTRAINT CK_BuildRunDetail_TriggerPayload_Json CHECK (TriggerPayload IS NULL OR ISJSON(TriggerPayload) = 1)
    );
END;
GO

IF OBJECT_ID(N'cicd.BuildStep', N'U') IS NULL
BEGIN
    CREATE TABLE cicd.BuildStep
    (
        BuildStepId     BIGINT          IDENTITY(1, 1) NOT NULL,
        BuildRunId      BIGINT          NOT NULL,
        StepOrder       INT             NOT NULL,
        Name            NVARCHAR(200)   NOT NULL,
        Status          VARCHAR(20)     NOT NULL CONSTRAINT DF_BuildStep_Status DEFAULT ('Pending'),
        StartedAt       DATETIME2(3)    NULL,
        FinishedAt      DATETIME2(3)    NULL,
        ExitCode        INT             NULL,
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_BuildStep_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_BuildStep_CreatedBy DEFAULT (SUSER_SNAME()),
        UpdatedAt       DATETIME2(3)    NULL,
        UpdatedBy       NVARCHAR(128)   NULL,
        IsDeleted       BIT             NOT NULL CONSTRAINT DF_BuildStep_IsDeleted DEFAULT (0),

        CONSTRAINT PK_BuildStep PRIMARY KEY CLUSTERED (BuildStepId),
        CONSTRAINT FK_BuildStep_BuildRun FOREIGN KEY (BuildRunId) REFERENCES cicd.BuildRun (BuildRunId) ON DELETE CASCADE,
        CONSTRAINT UQ_BuildStep_Run_Order UNIQUE (BuildRunId, StepOrder),
        CONSTRAINT CK_BuildStep_Status CHECK (Status IN ('Pending', 'Running', 'Succeeded', 'Failed', 'Skipped')),
        CONSTRAINT CK_BuildStep_StepOrder_Positive CHECK (StepOrder > 0),
        CONSTRAINT CK_BuildStep_Chronology CHECK (FinishedAt IS NULL OR (StartedAt IS NOT NULL AND FinishedAt >= StartedAt))
    );
END;
GO

IF OBJECT_ID(N'cicd.Artifact', N'U') IS NULL
BEGIN
    CREATE TABLE cicd.Artifact
    (
        ArtifactId       BIGINT          IDENTITY(1, 1) NOT NULL,
        PackageId        BIGINT          NOT NULL,
        PackageKey       NVARCHAR(300)   NOT NULL,
        PackageName      NVARCHAR(200)   NOT NULL,
        PublisherSlug    NVARCHAR(100)   NOT NULL,
        SemVer           NVARCHAR(50)    NOT NULL,
        SizeBytes        BIGINT          NOT NULL,
        Sha256           CHAR(64)        NOT NULL,
        StorageUri       NVARCHAR(500)   NOT NULL,
        LicensePrice     DECIMAL(18, 2)  NULL,
        RegistrySyncedAt DATETIME2(3)    NULL,
        CreatedAt        DATETIME2(3)    NOT NULL CONSTRAINT DF_Artifact_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy        NVARCHAR(128)   NOT NULL CONSTRAINT DF_Artifact_CreatedBy DEFAULT (SUSER_SNAME()),
        UpdatedAt        DATETIME2(3)    NULL,
        UpdatedBy        NVARCHAR(128)   NULL,
        IsDeleted        BIT             NOT NULL CONSTRAINT DF_Artifact_IsDeleted DEFAULT (0),
        RowVersion       ROWVERSION      NOT NULL,

        CONSTRAINT PK_Artifact PRIMARY KEY CLUSTERED (ArtifactId),
        CONSTRAINT UQ_Artifact_Sha256 UNIQUE (Sha256),
        CONSTRAINT CK_Artifact_PackageId_Positive CHECK (PackageId > 0),
        CONSTRAINT CK_Artifact_SizeBytes_Positive CHECK (SizeBytes > 0),
        CONSTRAINT CK_Artifact_Sha256_Hex CHECK (LEN(Sha256) = 64 AND Sha256 NOT LIKE '%[^0-9a-f]%'),
        CONSTRAINT CK_Artifact_PackageKey_Shape CHECK (PackageKey LIKE N'%_/_%' AND PackageKey NOT LIKE N'%/%/%'),
        CONSTRAINT CK_Artifact_PackageKey_MatchesCopy CHECK (PackageKey = CONCAT(PublisherSlug, N'/', PackageName)),
        CONSTRAINT CK_Artifact_LicensePrice_NonNegative CHECK (LicensePrice IS NULL OR LicensePrice >= 0)
    );
END;
GO

IF COL_LENGTH(N'cicd.Artifact', N'LicensePrice') IS NULL
BEGIN
    ALTER TABLE cicd.Artifact ADD LicensePrice DECIMAL(18, 2) NULL;
    EXEC(N'ALTER TABLE cicd.Artifact ADD CONSTRAINT CK_Artifact_LicensePrice_NonNegative CHECK (LicensePrice IS NULL OR LicensePrice >= 0);');
END;
GO

IF OBJECT_ID(N'cicd.ArtifactUsage', N'U') IS NULL
BEGIN
    CREATE TABLE cicd.ArtifactUsage
    (
        BuildRunId      BIGINT          NOT NULL,
        ArtifactId      BIGINT          NOT NULL,
        UsageKind       VARCHAR(20)     NOT NULL,
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_ArtifactUsage_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_ArtifactUsage_CreatedBy DEFAULT (SUSER_SNAME()),

        CONSTRAINT PK_ArtifactUsage PRIMARY KEY CLUSTERED (BuildRunId, ArtifactId, UsageKind),
        CONSTRAINT FK_ArtifactUsage_BuildRun FOREIGN KEY (BuildRunId) REFERENCES cicd.BuildRun (BuildRunId) ON DELETE CASCADE,
        CONSTRAINT FK_ArtifactUsage_Artifact FOREIGN KEY (ArtifactId) REFERENCES cicd.Artifact (ArtifactId),
        CONSTRAINT CK_ArtifactUsage_UsageKind CHECK (UsageKind IN ('Produced', 'Consumed'))
    );
END;
GO

IF OBJECT_ID(N'cicd.RunStatusHistory', N'U') IS NULL
BEGIN
    CREATE TABLE cicd.RunStatusHistory
    (
        RunStatusHistoryId BIGINT       IDENTITY(1, 1) NOT NULL,
        BuildRunId      BIGINT          NOT NULL,
        FromStatus      VARCHAR(20)     NULL,
        ToStatus        VARCHAR(20)     NOT NULL,
        Reason          NVARCHAR(400)   NULL,
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_RunStatusHistory_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_RunStatusHistory_CreatedBy DEFAULT (SUSER_SNAME()),

        CONSTRAINT PK_RunStatusHistory PRIMARY KEY CLUSTERED (RunStatusHistoryId),
        CONSTRAINT FK_RunStatusHistory_BuildRun FOREIGN KEY (BuildRunId) REFERENCES cicd.BuildRun (BuildRunId) ON DELETE CASCADE,
        CONSTRAINT CK_RunStatusHistory_ToStatus CHECK (ToStatus IN ('Queued', 'Running', 'Succeeded', 'Failed', 'Cancelled')),
        CONSTRAINT CK_RunStatusHistory_FromStatus CHECK (FromStatus IS NULL OR FromStatus IN ('Queued', 'Running', 'Succeeded', 'Failed', 'Cancelled')),
        CONSTRAINT CK_RunStatusHistory_Transition CHECK (FromStatus IS NULL OR FromStatus <> ToStatus)
    );
END;
GO

IF INDEXPROPERTY(OBJECT_ID(N'cicd.BuildRun'), N'IX_BuildRun_Pipeline_QueuedAt', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_BuildRun_Pipeline_QueuedAt
        ON cicd.BuildRun (PipelineId, QueuedAt DESC)
        INCLUDE (Status, RunNumber, FinishedAt)
        WHERE IsDeleted = 0;
GO

IF INDEXPROPERTY(OBJECT_ID(N'cicd.BuildRun'), N'IX_BuildRun_Status_QueuedAt', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_BuildRun_Status_QueuedAt
        ON cicd.BuildRun (Status, QueuedAt)
        INCLUDE (PipelineId, AgentId)
        WHERE IsDeleted = 0;
GO

IF INDEXPROPERTY(OBJECT_ID(N'cicd.BuildRun'), N'IX_BuildRun_AgentId', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_BuildRun_AgentId
        ON cicd.BuildRun (AgentId)
        WHERE AgentId IS NOT NULL AND IsDeleted = 0;
GO

IF INDEXPROPERTY(OBJECT_ID(N'cicd.Agent'), N'IX_Agent_Status_OperatingSystem', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_Agent_Status_OperatingSystem
        ON cicd.Agent (Status, OperatingSystem)
        INCLUDE (Hostname, LastHeartbeatAt)
        WHERE IsDeleted = 0;
GO

IF INDEXPROPERTY(OBJECT_ID(N'cicd.BuildStep'), N'IX_BuildStep_BuildRunId_StepOrder', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_BuildStep_BuildRunId_StepOrder
        ON cicd.BuildStep (BuildRunId, StepOrder)
        INCLUDE (Name, Status, ExitCode);
GO

IF INDEXPROPERTY(OBJECT_ID(N'cicd.RunStatusHistory'), N'IX_RunStatusHistory_BuildRunId_CreatedAt', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_RunStatusHistory_BuildRunId_CreatedAt
        ON cicd.RunStatusHistory (BuildRunId, CreatedAt);
GO

IF INDEXPROPERTY(OBJECT_ID(N'cicd.Artifact'), N'IX_Artifact_PackageId_SemVer', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_Artifact_PackageId_SemVer
        ON cicd.Artifact (PackageId, SemVer)
        INCLUDE (SizeBytes, StorageUri)
        WHERE IsDeleted = 0;
GO

IF INDEXPROPERTY(OBJECT_ID(N'cicd.ArtifactUsage'), N'IX_ArtifactUsage_ArtifactId_UsageKind', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_ArtifactUsage_ArtifactId_UsageKind
        ON cicd.ArtifactUsage (ArtifactId, UsageKind)
        INCLUDE (BuildRunId);
GO

CREATE OR ALTER VIEW cicd.vw_ActiveBuildRun
AS
SELECT
    r.BuildRunId,
    r.PipelineId,
    p.Name AS PipelineName,
    r.RunNumber,
    r.Status,
    r.QueuedAt,
    r.StartedAt,
    r.AgentId,
    a.Hostname AS AgentHostname,
    d.BranchName,
    d.CommitSha,
    d.TriggeredBy
FROM cicd.BuildRun AS r
INNER JOIN cicd.Pipeline AS p ON p.PipelineId = r.PipelineId
INNER JOIN cicd.BuildRunDetail AS d ON d.BuildRunId = r.BuildRunId
LEFT JOIN cicd.Agent AS a ON a.AgentId = r.AgentId
WHERE r.IsDeleted = 0
  AND r.Status IN ('Queued', 'Running');
GO

DECLARE @Description TABLE
(
    ObjectKind  VARCHAR(10)     NOT NULL,
    ObjectName  SYSNAME         NOT NULL,
    ChildKind   VARCHAR(10)     NULL,
    ChildName   SYSNAME         NULL,
    Text        NVARCHAR(400)   NOT NULL
);

INSERT INTO @Description (ObjectKind, ObjectName, ChildKind, ChildName, Text)
VALUES
    ('TABLE', N'Pipeline',         NULL,         NULL,                                       N'Build definition for one repository. Aggregate root.'),
    ('TABLE', N'Agent',            NULL,         NULL,                                       N'Build executor leased to one run at a time.'),
    ('TABLE', N'BuildRun',         NULL,         NULL,                                       N'One execution of a pipeline. Aggregate root of run, detail, steps and history.'),
    ('TABLE', N'BuildRunDetail',   NULL,         NULL,                                       N'1:1 extension of BuildRun: commit, branch and trigger data.'),
    ('TABLE', N'BuildStep',        NULL,         NULL,                                       N'Ordered step of a run.'),
    ('TABLE', N'Artifact',         NULL,         NULL,                                       N'Package binary produced or consumed by runs; outlives the run that produced it.'),
    ('TABLE', N'ArtifactUsage',    NULL,         NULL,                                       N'M:N junction between BuildRun and Artifact with its own column UsageKind.'),
    ('TABLE', N'RunStatusHistory', NULL,         NULL,                                       N'Append-only audit of every run status transition.'),
    ('TABLE', N'BuildRun',         'CONSTRAINT', N'FK_BuildRun_Pipeline',                    N'1:N Pipeline -> BuildRun. FK on the many side; no cascade, runs are history.'),
    ('TABLE', N'BuildRun',         'CONSTRAINT', N'FK_BuildRun_Agent',                       N'N:1 BuildRun -> Agent. Nullable while the run is queued.'),
    ('TABLE', N'BuildRunDetail',   'CONSTRAINT', N'FK_BuildRunDetail_BuildRun',              N'1:1 BuildRun <-> BuildRunDetail. The FK is also the PK of the dependent table; cascade because a detail cannot exist without its run.'),
    ('TABLE', N'BuildStep',        'CONSTRAINT', N'FK_BuildStep_BuildRun',                   N'1:N BuildRun -> BuildStep. Cascade because steps cannot exist without their run.'),
    ('TABLE', N'ArtifactUsage',    'CONSTRAINT', N'FK_ArtifactUsage_BuildRun',               N'M:N BuildRun <-> Artifact, run side. Cascade with the run.'),
    ('TABLE', N'ArtifactUsage',    'CONSTRAINT', N'FK_ArtifactUsage_Artifact',               N'M:N BuildRun <-> Artifact, artefact side. No cascade: an artefact outlives runs that used it.'),
    ('TABLE', N'RunStatusHistory', 'CONSTRAINT', N'FK_RunStatusHistory_BuildRun',            N'1:N BuildRun -> RunStatusHistory.'),
    ('TABLE', N'Pipeline',         'COLUMN',     N'PackageId',                               N'Reference to registry.Package by its Id. Plain column, no FOREIGN KEY: the package lives in DevHub_Registry.'),
    ('TABLE', N'Pipeline',         'COLUMN',     N'PackageKey',                              N'Readable "<publisher>/<name>" of the package in PackageId. Owner: Registry. Policy: refreshed by Registry events.'),
    ('TABLE', N'Artifact',         'COLUMN',     N'PackageId',                               N'Reference to registry.Package by its Id. Plain column, no FOREIGN KEY across databases.'),
    ('TABLE', N'Artifact',         'COLUMN',     N'PackageKey',                              N'Readable "<publisher>/<name>" of the package in PackageId, composed from the copied PublisherSlug and PackageName. Policy: snapshot.'),
    ('TABLE', N'Artifact',         'COLUMN',     N'PackageName',                             N'Copy of registry.Package.Name. Owner: Registry. Policy: snapshot; a published version never changes.'),
    ('TABLE', N'Artifact',         'COLUMN',     N'PublisherSlug',                           N'Copy of registry.Publisher.Slug. Owner: Registry. Policy: snapshot.'),
    ('TABLE', N'Artifact',         'COLUMN',     N'SemVer',                                  N'Copy of registry.PackageVersion.SemVer. Owner: Registry. Policy: snapshot.'),
    ('TABLE', N'Artifact',         'COLUMN',     N'RegistrySyncedAt',                        N'Last time the copied columns were confirmed by a Registry event: cicd.Package.SyncedAt when the snapshot was taken.'),
    ('TABLE', N'Artifact',         'COLUMN',     N'LicensePrice',                            N'Copy of registry.Package.LicensePrice taken from cicd.Package when the run completed. Owner: Registry. Policy: snapshot, never updated; NULL for artefacts built before the copy existed.'),
    ('TABLE', N'Package',          NULL,         NULL,                                       N'Local copy of registry.Package for this context''s own queries. Owner: Registry. Policy: refreshed by Registry events (queue cicd.registry-sync).'),
    ('TABLE', N'Package',          'COLUMN',     N'PackageId',                               N'Id of registry.Package; no IDENTITY, the key is assigned by the Registry. No FOREIGN KEY across databases.'),
    ('TABLE', N'Package',          'COLUMN',     N'SourceChangedAt',                         N'OccurredAt of the last applied Registry event; an older event is ignored (out-of-order delivery). NULL for seed rows.'),
    ('TABLE', N'Package',          'COLUMN',     N'SyncedAt',                                N'When this copy was last written.'),
    ('TABLE', N'ProcessedEvent',   NULL,         NULL,                                       N'Idempotency ledger: EventId of every handled integration event, written in the same transaction as the change it caused.'),
    ('TABLE', N'BuildRun',         'COLUMN',     N'RowVersion',                              N'Optimistic concurrency token checked by usp_TransitionRunStatus.'),
    ('TABLE', N'BuildRun',         'INDEX',      N'IX_BuildRun_Pipeline_QueuedAt',           N'Query: run history of one pipeline, newest first, keyset-paged. Filtered to live rows, covers the list columns.'),
    ('TABLE', N'BuildRun',         'INDEX',      N'IX_BuildRun_Status_QueuedAt',             N'Query: dispatcher scan of the oldest queued runs and the vw_ActiveBuildRun board.'),
    ('TABLE', N'BuildRun',         'INDEX',      N'IX_BuildRun_AgentId',                     N'Query: which run an agent is executing, used when releasing the agent. Filtered: queued runs have no agent.'),
    ('TABLE', N'Agent',            'INDEX',      N'IX_Agent_Status_OperatingSystem',         N'Query: usp_LeaseAgent picks the first idle agent for an operating system.'),
    ('TABLE', N'BuildStep',        'INDEX',      N'IX_BuildStep_BuildRunId_StepOrder',       N'Query: usp_GetRunTimeline step list in StepOrder, covered without key lookups.'),
    ('TABLE', N'RunStatusHistory', 'INDEX',      N'IX_RunStatusHistory_BuildRunId_CreatedAt', N'Query: usp_GetRunTimeline status history in chronological order.'),
    ('TABLE', N'Artifact',         'INDEX',      N'IX_Artifact_PackageId_SemVer',            N'Query: all artefacts of one Registry package by version; also the Registry-event refresh of copied columns by PackageId.'),
    ('TABLE', N'ArtifactUsage',    'INDEX',      N'IX_ArtifactUsage_ArtifactId_UsageKind',   N'Query: reverse M:N lookup, which runs produced or consumed an artefact. The PK only serves run -> artefact.'),
    ('VIEW',  N'vw_ActiveBuildRun', NULL,        NULL,                                       N'Denormalised read model: queued and running builds with pipeline, agent and commit in one row.');

DECLARE @ObjectKind VARCHAR(10), @ObjectName SYSNAME, @ChildKind VARCHAR(10), @ChildName SYSNAME, @Text NVARCHAR(400);

DECLARE description_cursor CURSOR LOCAL FAST_FORWARD FOR
    SELECT ObjectKind, ObjectName, ChildKind, ChildName, Text FROM @Description;

OPEN description_cursor;
FETCH NEXT FROM description_cursor INTO @ObjectKind, @ObjectName, @ChildKind, @ChildName, @Text;

WHILE @@FETCH_STATUS = 0
BEGIN
    IF EXISTS
    (
        SELECT 1
        FROM sys.fn_listextendedproperty(N'MS_Description', N'SCHEMA', N'cicd', @ObjectKind, @ObjectName, @ChildKind, @ChildName)
    )
        EXEC sys.sp_updateextendedproperty
            @name = N'MS_Description', @value = @Text,
            @level0type = N'SCHEMA', @level0name = N'cicd',
            @level1type = @ObjectKind, @level1name = @ObjectName,
            @level2type = @ChildKind, @level2name = @ChildName;
    ELSE
        EXEC sys.sp_addextendedproperty
            @name = N'MS_Description', @value = @Text,
            @level0type = N'SCHEMA', @level0name = N'cicd',
            @level1type = @ObjectKind, @level1name = @ObjectName,
            @level2type = @ChildKind, @level2name = @ChildName;

    FETCH NEXT FROM description_cursor INTO @ObjectKind, @ObjectName, @ChildKind, @ChildName, @Text;
END;

CLOSE description_cursor;
DEALLOCATE description_cursor;
GO
