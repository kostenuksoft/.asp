USE DevHub_Cicd;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

DELETE u
FROM cicd.ArtifactUsage AS u
INNER JOIN cicd.BuildRun AS r ON r.BuildRunId = u.BuildRunId
WHERE r.CreatedBy = N'verify';

DELETE FROM cicd.BuildRun WHERE CreatedBy = N'verify';
DELETE FROM cicd.Artifact WHERE CreatedBy = N'verify';
UPDATE cicd.Agent SET Status = 'Idle', UpdatedAt = NULL, UpdatedBy = NULL WHERE UpdatedBy = N'verify';
GO

IF OBJECT_ID(N'tempdb..#Check') IS NOT NULL DROP TABLE #Check;
CREATE TABLE #Check (CheckId INT IDENTITY(1, 1), Name NVARCHAR(200), Passed BIT, Detail NVARCHAR(400));
GO

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'no foreign key leaves this database',
       CASE WHEN COUNT(*) = 0 THEN 1 ELSE 0 END,
       CONCAT(COUNT(*), N' foreign key(s) reference objects outside DevHub_Cicd')
FROM sys.foreign_keys AS fk
WHERE NOT EXISTS (SELECT 1 FROM sys.objects AS o WHERE o.object_id = fk.referenced_object_id);

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'cross-context columns carry no foreign key',
       CASE WHEN COUNT(*) = 0 THEN 1 ELSE 0 END,
       CONCAT(COUNT(*), N' FK column(s) among PackageId, PackageKey, PackageName, PublisherSlug, SemVer')
FROM sys.foreign_key_columns AS fkc
INNER JOIN sys.columns AS c ON c.object_id = fkc.parent_object_id AND c.column_id = fkc.parent_column_id
WHERE c.name IN (N'PackageId', N'PackageKey', N'PackageName', N'PublisherSlug', N'SemVer');

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'the Registry package is referenced by its Id in a plain column',
       CASE WHEN COUNT(*) = 2 THEN 1 ELSE 0 END,
       CONCAT(COUNT(*), N' of 2 PackageId BIGINT columns (Pipeline, Artifact)')
FROM sys.columns AS c
WHERE c.name = N'PackageId'
  AND c.system_type_id = TYPE_ID(N'bigint')
  AND c.object_id IN (OBJECT_ID(N'cicd.Pipeline'), OBJECT_ID(N'cicd.Artifact'));

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'copied columns are documented beside the column',
       CASE WHEN COUNT(*) = 5 THEN 1 ELSE 0 END,
       CONCAT(COUNT(*), N' of 5 Artifact reference/copy columns have MS_Description')
FROM sys.fn_listextendedproperty(N'MS_Description', N'SCHEMA', N'cicd', N'TABLE', N'Artifact', N'COLUMN', NULL)
WHERE objname IN (N'PackageId', N'PackageKey', N'PackageName', N'PublisherSlug', N'SemVer');

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'every index states the query it serves',
       CASE WHEN SUM(CASE WHEN ep.value IS NULL THEN 1 ELSE 0 END) = 0 THEN 1 ELSE 0 END,
       CONCAT(COUNT(*), N' IX_ index(es), ', SUM(CASE WHEN ep.value IS NULL THEN 1 ELSE 0 END), N' without MS_Description')
FROM sys.indexes AS i
INNER JOIN sys.tables AS t ON t.object_id = i.object_id AND t.schema_id = SCHEMA_ID(N'cicd')
LEFT JOIN sys.extended_properties AS ep
    ON ep.major_id = i.object_id AND ep.minor_id = i.index_id AND ep.class = 7 AND ep.name = N'MS_Description'
WHERE i.name LIKE N'IX[_]%';

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'1:1 BuildRunDetail: FK is also the PK',
       CASE WHEN COUNT(*) = 1 THEN 1 ELSE 0 END,
       N'PK_BuildRunDetail and FK_BuildRunDetail_BuildRun share BuildRunId'
FROM sys.index_columns AS ic
INNER JOIN sys.indexes AS i ON i.object_id = ic.object_id AND i.index_id = ic.index_id AND i.is_primary_key = 1
INNER JOIN sys.foreign_key_columns AS fkc ON fkc.parent_object_id = ic.object_id AND fkc.parent_column_id = ic.column_id
WHERE ic.object_id = OBJECT_ID(N'cicd.BuildRunDetail');

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'M:N ArtifactUsage: junction with two FKs',
       CASE WHEN COUNT(DISTINCT referenced_object_id) = 2 THEN 1 ELSE 0 END,
       N'FKs to BuildRun and Artifact'
FROM sys.foreign_keys
WHERE parent_object_id = OBJECT_ID(N'cicd.ArtifactUsage');
GO

DECLARE @PipelineId BIGINT = (SELECT PipelineId FROM cicd.Pipeline WHERE Name = N'billing-sdk CI');
DECLARE @RunId BIGINT;

EXEC cicd.usp_QueueBuildRun
    @PipelineId     = @PipelineId,
    @CommitSha      = '9e8d7c6b5a4f3e2d1c0b9a8f7e6d5c4b3a2f1e0d',
    @CommitMessage  = N'Introduce refund calculation',
    @BranchName     = N'develop',
    @TriggeredBy    = N'verify',
    @TriggerKind    = 'Push',
    @TriggerPayload = N'{"ref":"refs/heads/develop"}',
    @BuildRunId     = @RunId OUTPUT;

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'queue: run, detail and history written in one transaction',
       CASE WHEN r.Status = 'Queued' AND d.BuildRunId IS NOT NULL AND h.ToStatus = 'Queued' THEN 1 ELSE 0 END,
       CONCAT(N'run ', @RunId, N' number ', r.RunNumber)
FROM cicd.BuildRun AS r
LEFT JOIN cicd.BuildRunDetail AS d ON d.BuildRunId = r.BuildRunId
LEFT JOIN cicd.RunStatusHistory AS h ON h.BuildRunId = r.BuildRunId
WHERE r.BuildRunId = @RunId;
GO

DECLARE @RunId BIGINT = (SELECT MAX(BuildRunId) FROM cicd.BuildRun WHERE CreatedBy = N'verify');
DECLARE @AgentId BIGINT;

EXEC cicd.usp_LeaseAgent
    @BuildRunId      = @RunId,
    @OperatingSystem = N'Linux',
    @LeasedBy        = N'verify',
    @AgentId         = @AgentId OUTPUT;

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'lease: queued run gets an idle agent and starts',
       CASE WHEN r.Status = 'Running' AND r.AgentId = @AgentId AND a.Status = 'Leased' THEN 1 ELSE 0 END,
       CONCAT(N'agent ', a.Hostname)
FROM cicd.BuildRun AS r
INNER JOIN cicd.Agent AS a ON a.AgentId = r.AgentId
WHERE r.BuildRunId = @RunId;
GO

DECLARE @RunId BIGINT = (SELECT MAX(BuildRunId) FROM cicd.BuildRun WHERE CreatedBy = N'verify');
DECLARE @Error INT = 0;

BEGIN TRY
    EXEC cicd.usp_TransitionRunStatus @BuildRunId = @RunId, @ToStatus = 'Queued', @ChangedBy = N'verify';
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
END CATCH;

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'illegal transition Running -> Queued is refused with a managed error',
       CASE WHEN @Error = 50022 AND Status = 'Running' THEN 1 ELSE 0 END,
       CONCAT(N'error ', @Error, N', status still ', Status)
FROM cicd.BuildRun
WHERE BuildRunId = @RunId;
GO

DECLARE @RunId BIGINT = (SELECT MAX(BuildRunId) FROM cicd.BuildRun WHERE CreatedBy = N'verify');
DECLARE @Error INT = 0;

BEGIN TRY
    EXEC cicd.usp_TransitionRunStatus
        @BuildRunId = @RunId, @ToStatus = 'Cancelled', @ChangedBy = N'verify',
        @ExpectedRowVersion = 0x0000000000000001;
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
END CATCH;

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'stale RowVersion is refused (optimistic concurrency)',
       CASE WHEN @Error = 50021 AND Status = 'Running' THEN 1 ELSE 0 END,
       CONCAT(N'error ', @Error)
FROM cicd.BuildRun
WHERE BuildRunId = @RunId;
GO

DECLARE @RunId BIGINT = (SELECT MAX(BuildRunId) FROM cicd.BuildRun WHERE CreatedBy = N'verify');
DECLARE @Error INT = 0;

BEGIN TRY
    EXEC cicd.usp_DeleteBuildRun @BuildRunId = @RunId, @DeletedBy = N'verify';
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
END CATCH;

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'an active run cannot be deleted',
       CASE WHEN @Error = 50052 AND IsDeleted = 0 THEN 1 ELSE 0 END,
       CONCAT(N'error ', @Error)
FROM cicd.BuildRun
WHERE BuildRunId = @RunId;
GO

DECLARE @RunId BIGINT = (SELECT MAX(BuildRunId) FROM cicd.BuildRun WHERE CreatedBy = N'verify');
DECLARE @AgentId BIGINT = (SELECT AgentId FROM cicd.BuildRun WHERE BuildRunId = @RunId);

INSERT INTO cicd.BuildStep (BuildRunId, StepOrder, Name, Status, StartedAt, FinishedAt, ExitCode, CreatedBy)
VALUES
    (@RunId, 1, N'Restore', 'Succeeded', SYSUTCDATETIME(), SYSUTCDATETIME(), 0, N'verify'),
    (@RunId, 2, N'Build',   'Succeeded', SYSUTCDATETIME(), SYSUTCDATETIME(), 0, N'verify'),
    (@RunId, 3, N'Pack',    'Pending',   NULL, NULL, NULL, N'verify');

DECLARE @Produced cicd.ArtifactReference;
INSERT INTO @Produced (PackageId, PackageKey, PackageName, PublisherSlug, SemVer, SizeBytes, Sha256, StorageUri)
VALUES (1, N'payments-team/billing-sdk', N'billing-sdk', N'payments-team', N'0.9.2', 540672,
        'a0b1c2d3e4f5a6b7c8d9e0f1a2b3c4d5e6f7a8b9c0d1e2f3a4b5c6d7e8f9a0b1',
        N'https://artifacts.devhub.local/billing/0.9.2.nupkg');

EXEC cicd.usp_CompleteBuildRun @BuildRunId = @RunId, @CompletedBy = N'verify', @ProducedArtifacts = @Produced;

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'complete: run, steps, artefact, usage and agent change atomically',
       CASE
           WHEN r.Status = 'Succeeded'
            AND a.Status = 'Idle'
            AND s.Status = 'Skipped'
            AND EXISTS (SELECT 1 FROM cicd.ArtifactUsage AS u INNER JOIN cicd.Artifact AS x ON x.ArtifactId = u.ArtifactId
                        WHERE u.BuildRunId = @RunId AND u.UsageKind = 'Produced' AND x.SemVer = N'0.9.2')
           THEN 1 ELSE 0
       END,
       CONCAT(N'run ', r.Status, N', agent ', a.Status, N', pending step ', s.Status)
FROM cicd.BuildRun AS r
INNER JOIN cicd.Agent AS a ON a.AgentId = @AgentId
INNER JOIN cicd.BuildStep AS s ON s.BuildRunId = r.BuildRunId AND s.StepOrder = 3
WHERE r.BuildRunId = @RunId;
GO

DECLARE @PipelineId BIGINT = (SELECT PipelineId FROM cicd.Pipeline WHERE Name = N'billing-sdk CI');
DECLARE @RunId BIGINT;

EXEC cicd.usp_QueueBuildRun
    @PipelineId = @PipelineId, @CommitSha = 'aaaabbbbccccddddeeeeffff0000111122223333',
    @CommitMessage = N'Queued then cancelled', @BranchName = N'develop',
    @TriggeredBy = N'verify', @TriggerKind = 'Manual', @BuildRunId = @RunId OUTPUT;

EXEC cicd.usp_TransitionRunStatus @BuildRunId = @RunId, @ToStatus = 'Cancelled', @Reason = N'verify', @ChangedBy = N'verify';

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'legal transition Queued -> Cancelled leaves no finish time',
       CASE WHEN Status = 'Cancelled' AND StartedAt IS NULL AND FinishedAt IS NULL THEN 1 ELSE 0 END,
       CONCAT(N'status ', Status)
FROM cicd.BuildRun
WHERE BuildRunId = @RunId;

EXEC cicd.usp_DeleteBuildRun @BuildRunId = @RunId, @DeletedBy = N'verify';

DECLARE @Error INT = 0;
BEGIN TRY
    EXEC cicd.usp_GetRunTimeline @BuildRunId = @RunId;
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
END CATCH;

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'soft delete hides the run and keeps the row',
       CASE WHEN r.IsDeleted = 1 AND d.IsDeleted = 1 AND @Error = 50040 THEN 1 ELSE 0 END,
       CONCAT(N'timeline error ', @Error)
FROM cicd.BuildRun AS r
INNER JOIN cicd.BuildRunDetail AS d ON d.BuildRunId = r.BuildRunId
WHERE r.BuildRunId = @RunId;
GO

DECLARE @PipelineId BIGINT = (SELECT PipelineId FROM cicd.Pipeline WHERE Name = N'billing-sdk CI');
DECLARE @RunId BIGINT;
DECLARE @Error INT = 0;

UPDATE cicd.Pipeline SET IsEnabled = 0 WHERE PipelineId = @PipelineId;

BEGIN TRY
    EXEC cicd.usp_QueueBuildRun
        @PipelineId = @PipelineId, @CommitSha = 'bbbbccccddddeeeeffff00001111222233334444',
        @CommitMessage = N'Should not queue', @BranchName = N'develop',
        @TriggeredBy = N'verify', @TriggerKind = 'Manual', @BuildRunId = @RunId OUTPUT;
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
END CATCH;

UPDATE cicd.Pipeline SET IsEnabled = 1 WHERE PipelineId = @PipelineId;

INSERT INTO #Check (Name, Passed, Detail)
VALUES (N'a disabled pipeline refuses to queue', CASE WHEN @Error = 50002 THEN 1 ELSE 0 END, CONCAT(N'error ', @Error));
GO

DECLARE @Error INT = 0;
BEGIN TRY
    INSERT INTO cicd.Agent (Hostname, OperatingSystem, Status) VALUES (N'verify-bad-agent', N'Windows', 'Sleeping');
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
END CATCH;

INSERT INTO #Check (Name, Passed, Detail)
VALUES (N'CHECK rejects an unknown agent status', CASE WHEN @Error = 547 THEN 1 ELSE 0 END, CONCAT(N'error ', @Error));

SET @Error = 0;
BEGIN TRY
    INSERT INTO cicd.Agent (Hostname, OperatingSystem) VALUES (N'build-win-01', N'Windows');
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
END CATCH;

INSERT INTO #Check (Name, Passed, Detail)
VALUES (N'UNIQUE rejects a duplicate agent hostname', CASE WHEN @Error = 2627 THEN 1 ELSE 0 END, CONCAT(N'error ', @Error));

SET @Error = 0;
BEGIN TRY
    INSERT INTO cicd.Artifact (PackageId, PackageKey, PackageName, PublisherSlug, SemVer, SizeBytes, Sha256, StorageUri)
    VALUES (2, N'platform-team/devhub-auth', N'other-name', N'platform-team', N'9.9.9', 1,
            'ffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffffff', N'https://x');
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
    IF XACT_STATE() <> 0 ROLLBACK TRANSACTION;
END CATCH;

INSERT INTO #Check (Name, Passed, Detail)
VALUES (N'CHECK keeps PackageKey consistent with its copied parts', CASE WHEN @Error = 547 THEN 1 ELSE 0 END, CONCAT(N'error ', @Error));
GO

DECLARE @PageSize INT = 2;
DECLARE @Page1 TABLE (BuildRunId BIGINT);
DECLARE @Page2 TABLE (BuildRunId BIGINT);

INSERT INTO @Page1
SELECT TOP (@PageSize) r.BuildRunId
FROM cicd.BuildRun AS r
INNER JOIN cicd.BuildRunDetail AS d ON d.BuildRunId = r.BuildRunId
WHERE r.IsDeleted = 0
ORDER BY r.BuildRunId DESC;

DECLARE @After BIGINT = (SELECT MIN(BuildRunId) FROM @Page1);

INSERT INTO @Page2
SELECT TOP (@PageSize) r.BuildRunId
FROM cicd.BuildRun AS r
WHERE r.IsDeleted = 0
  AND r.BuildRunId < @After
ORDER BY r.BuildRunId DESC;

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'keyset pagination returns disjoint, ordered pages',
       CASE WHEN (SELECT COUNT(*) FROM @Page1) = 2
             AND (SELECT COUNT(*) FROM @Page2) BETWEEN 1 AND 2
             AND (SELECT MAX(BuildRunId) FROM @Page2) < @After
            THEN 1 ELSE 0 END,
       CONCAT(N'page 1 ends at ', @After, N', page 2 starts at ', (SELECT MAX(BuildRunId) FROM @Page2));

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'search: failed runs of one pipeline through the covering index',
       CASE WHEN COUNT(*) >= 1 THEN 1 ELSE 0 END,
       CONCAT(COUNT(*), N' failed run(s) of devhub-auth CI')
FROM cicd.BuildRun AS r
INNER JOIN cicd.Pipeline AS p ON p.PipelineId = r.PipelineId
WHERE p.Name = N'devhub-auth CI'
  AND r.Status = 'Failed'
  AND r.IsDeleted = 0;

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'view shows only queued and running builds',
       CASE WHEN SUM(CASE WHEN Status NOT IN ('Queued', 'Running') THEN 1 ELSE 0 END) = 0 AND COUNT(*) >= 1 THEN 1 ELSE 0 END,
       CONCAT(COUNT(*), N' active run(s)')
FROM cicd.vw_ActiveBuildRun;
GO

IF DATABASE_PRINCIPAL_ID(N'cicd_reader') IS NOT NULL AND DATABASE_PRINCIPAL_ID(N'cicd_app') IS NOT NULL AND SUSER_ID(N'devhub_cicd_app') IS NOT NULL
BEGIN
    DECLARE @ReaderError INT = 0, @AppRows INT = -1, @CrossError INT = 0;
    DECLARE @LoginMapped BIT =
        CASE WHEN EXISTS
        (
            SELECT 1
            FROM sys.database_principals AS u
            INNER JOIN sys.server_principals AS l ON l.sid = u.sid
            WHERE u.name = N'cicd_app' AND l.name = N'devhub_cicd_app' AND l.default_database_name = N'DevHub_Cicd'
        ) THEN 1 ELSE 0 END;
    DECLARE @OtherDatabase SYSNAME = CASE WHEN DB_ID(N'DevHub_Registry') IS NOT NULL THEN N'DevHub_Registry' ELSE N'model' END;
    DECLARE @CrossProbe NVARCHAR(400) = CONCAT(N'SELECT COUNT(*) FROM ', QUOTENAME(@OtherDatabase), N'.sys.objects;');

    EXECUTE AS USER = N'cicd_reader';
    BEGIN TRY
        DELETE FROM cicd.Agent WHERE 1 = 0;
    END TRY
    BEGIN CATCH
        SET @ReaderError = ERROR_NUMBER();
    END CATCH;
    REVERT;

    EXECUTE AS LOGIN = N'devhub_cicd_app';
    SET @AppRows = (SELECT COUNT(*) FROM cicd.Agent);
    BEGIN TRY
        EXEC(@CrossProbe);
    END TRY
    BEGIN CATCH
        SET @CrossError = ERROR_NUMBER();
    END CATCH;
    REVERT;

    INSERT INTO #Check (Name, Passed, Detail)
    VALUES
        (N'least privilege: the reader cannot write', CASE WHEN @ReaderError = 229 THEN 1 ELSE 0 END, CONCAT(N'error ', @ReaderError)),
        (N'least privilege: a separate app login is mapped to this database', @LoginMapped, N'devhub_cicd_app -> cicd_app, default database DevHub_Cicd'),
        (N'least privilege: the app login reaches its own database only', CASE WHEN @AppRows >= 0 AND @CrossError = 916 THEN 1 ELSE 0 END, CONCAT(N'error ', @CrossError, N' reading ', @OtherDatabase));
END
ELSE
    INSERT INTO #Check (Name, Passed, Detail)
    VALUES (N'least privilege: login and roles exist (run security.sql before verify.sql)', 0, N'devhub_cicd_app / cicd_app / cicd_reader missing');
GO

DELETE u
FROM cicd.ArtifactUsage AS u
INNER JOIN cicd.BuildRun AS r ON r.BuildRunId = u.BuildRunId
WHERE r.CreatedBy = N'verify';

DELETE FROM cicd.BuildRun WHERE CreatedBy = N'verify';
DELETE FROM cicd.Artifact WHERE CreatedBy = N'verify';
UPDATE cicd.Agent SET Status = 'Idle', UpdatedAt = NULL, UpdatedBy = NULL WHERE UpdatedBy = N'verify';
GO

SELECT CheckId, CASE Passed WHEN 1 THEN 'PASS' ELSE 'FAIL' END AS Result, Name, Detail
FROM #Check
ORDER BY CheckId;

DECLARE @Failed INT = (SELECT COUNT(*) FROM #Check WHERE Passed = 0);
DECLARE @Total INT = (SELECT COUNT(*) FROM #Check);
PRINT CONCAT(@Total - @Failed, N'/', @Total, N' checks passed');

IF @Failed > 0
    THROW 50099, N'Verification failed.', 1;
GO
