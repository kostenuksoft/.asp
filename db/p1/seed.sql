USE DevHub_Cicd;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

INSERT INTO cicd.Package (PackageId, PublisherSlug, PackageName, PackageKey, LicensePrice, CreatedBy)
SELECT v.PackageId, v.PublisherSlug, v.PackageName, CONCAT(v.PublisherSlug, N'/', v.PackageName), v.LicensePrice, N'seed'
FROM
(
    VALUES
        (1, N'payments-team', N'billing-sdk',    49.00),
        (2, N'platform-team', N'devhub-auth',     0.00),
        (3, N'platform-team', N'devhub-storage',  0.00)
) AS v (PackageId, PublisherSlug, PackageName, LicensePrice)
WHERE NOT EXISTS
(
    SELECT 1 FROM cicd.Package AS p WHERE p.PackageId = v.PackageId
);
GO

INSERT INTO cicd.Pipeline (Name, RepositoryUrl, DefaultBranch, PackageId, PackageKey, CreatedBy)
SELECT v.Name, v.RepositoryUrl, v.DefaultBranch, v.PackageId, v.PackageKey, N'seed'
FROM
(
    VALUES
        (N'devhub-auth CI',    N'https://git.devhub.local/platform-team/devhub-auth',    N'main',    2, N'platform-team/devhub-auth'),
        (N'devhub-storage CI', N'https://git.devhub.local/platform-team/devhub-storage', N'main',    3, N'platform-team/devhub-storage'),
        (N'billing-sdk CI',    N'https://git.devhub.local/payments-team/billing-sdk',    N'develop', 1, N'payments-team/billing-sdk')
) AS v (Name, RepositoryUrl, DefaultBranch, PackageId, PackageKey)
WHERE NOT EXISTS
(
    SELECT 1 FROM cicd.Pipeline AS p WHERE p.RepositoryUrl = v.RepositoryUrl AND p.Name = v.Name
);
GO

INSERT INTO cicd.Agent (Hostname, OperatingSystem, Status, LastHeartbeatAt, CreatedBy)
SELECT v.Hostname, v.OperatingSystem, v.Status, CASE WHEN v.Status = 'Offline' THEN NULL ELSE SYSUTCDATETIME() END, N'seed'
FROM
(
    VALUES
        (N'build-win-01',   N'Windows', 'Idle'),
        (N'build-win-02',   N'Windows', 'Idle'),
        (N'build-linux-01', N'Linux',   'Idle'),
        (N'build-linux-02', N'Linux',   'Offline'),
        (N'build-mac-01',   N'macOS',   'Idle')
) AS v (Hostname, OperatingSystem, Status)
WHERE NOT EXISTS
(
    SELECT 1 FROM cicd.Agent AS a WHERE a.Hostname = v.Hostname
);
GO

INSERT INTO cicd.Artifact (PackageId, PackageKey, PackageName, PublisherSlug, SemVer, SizeBytes, Sha256, StorageUri, LicensePrice, RegistrySyncedAt, CreatedBy)
SELECT v.PackageId, CONCAT(v.PublisherSlug, N'/', v.PackageName), v.PackageName, v.PublisherSlug, v.SemVer, v.SizeBytes, v.Sha256, v.StorageUri, c.LicensePrice, c.SyncedAt, N'seed'
FROM
(
    VALUES
        (2, N'devhub-auth',    N'platform-team', N'2.0.0', 1048576, '3b1f9c2a7d4e8f0b6c5a9e2d7f1b4c8a0e3d6f9b2c5a8e1d4f7b0c3a6e9d2f5b', N'https://artifacts.devhub.local/auth/2.0.0.nupkg'),
        (2, N'devhub-auth',    N'platform-team', N'2.1.0', 1130496, '9f2e5d8c1b4a7e0d3c6f9b2e5a8d1c4f7b0e3a6d9c2f5b8e1a4d7c0f3b6e9a2d', N'https://artifacts.devhub.local/auth/2.1.0.nupkg'),
        (3, N'devhub-storage', N'platform-team', N'1.4.2', 2097152, 'c4a7e0d3b6f9c2e5a8d1b4f7e0a3d6c9b2e5f8a1d4c7b0e3f6a9d2c5b8e1f4a7', N'https://artifacts.devhub.local/storage/1.4.2.nupkg'),
        (1, N'billing-sdk',    N'payments-team', N'0.9.1',  524288, 'e1d4c7b0a3f6e9d2c5b8a1f4e7d0c3b6a9f2e5d8c1b4a7f0e3d6c9b2a5f8e1d4', N'https://artifacts.devhub.local/billing/0.9.1.nupkg')
) AS v (PackageId, PackageName, PublisherSlug, SemVer, SizeBytes, Sha256, StorageUri)
LEFT JOIN cicd.Package AS c ON c.PackageId = v.PackageId
WHERE NOT EXISTS
(
    SELECT 1 FROM cicd.Artifact AS a WHERE a.Sha256 = v.Sha256
);
GO

DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

INSERT INTO cicd.BuildRun (PipelineId, AgentId, RunNumber, Status, QueuedAt, StartedAt, FinishedAt, CreatedBy)
SELECT p.PipelineId, a.AgentId, v.RunNumber, v.Status,
       DATEADD(MINUTE, -v.QueuedMinutesAgo, @Now),
       DATEADD(MINUTE, -v.StartedMinutesAgo, @Now),
       DATEADD(MINUTE, -v.FinishedMinutesAgo, @Now),
       N'seed'
FROM
(
    VALUES
        (N'devhub-auth CI',    1, 'Succeeded', N'build-win-01', 360,  359,  352),
        (N'devhub-auth CI',    2, 'Failed',    N'build-win-01', 180,  179,  175),
        (N'devhub-storage CI', 1, 'Queued',    NULL,              4, NULL, NULL),
        (N'billing-sdk CI',    1, 'Cancelled', NULL,             90, NULL, NULL)
) AS v (PipelineName, RunNumber, Status, AgentHostname, QueuedMinutesAgo, StartedMinutesAgo, FinishedMinutesAgo)
INNER JOIN cicd.Pipeline AS p ON p.Name = v.PipelineName
LEFT JOIN cicd.Agent AS a ON a.Hostname = v.AgentHostname
WHERE NOT EXISTS
(
    SELECT 1 FROM cicd.BuildRun AS r WHERE r.PipelineId = p.PipelineId AND r.RunNumber = v.RunNumber
)
ORDER BY v.QueuedMinutesAgo DESC;
GO

INSERT INTO cicd.BuildRunDetail (BuildRunId, CommitSha, CommitMessage, BranchName, TriggeredBy, TriggerKind, TriggerPayload, CreatedBy)
SELECT r.BuildRunId, v.CommitSha, v.CommitMessage, v.BranchName, v.TriggeredBy, v.TriggerKind, v.TriggerPayload, N'seed'
FROM
(
    VALUES
        (N'devhub-auth CI',    1, '1a2b3c4d5e6f708192a3b4c5d6e7f8091a2b3c4d', N'Add token refresh endpoint',  N'main',           N'o.kovalenko', 'Push',        N'{"ref":"refs/heads/main","pusher":"o.kovalenko"}'),
        (N'devhub-auth CI',    2, '2b3c4d5e6f708192a3b4c5d6e7f8091a2b3c4d5e', N'Refactor claim validation',   N'feature/claims', N'i.petrenko',  'PullRequest', N'{"pullRequest":128,"targetBranch":"main"}'),
        (N'devhub-storage CI', 1, '3c4d5e6f708192a3b4c5d6e7f8091a2b3c4d5e6f', N'Bump storage client timeout', N'main',           N'scheduler',   'Schedule',    NULL),
        (N'billing-sdk CI',    1, '4d5e6f708192a3b4c5d6e7f8091a2b3c4d5e6f70', N'Draft refund rounding rules', N'develop',        N'm.shevchuk',  'Manual',      NULL)
) AS v (PipelineName, RunNumber, CommitSha, CommitMessage, BranchName, TriggeredBy, TriggerKind, TriggerPayload)
INNER JOIN cicd.Pipeline AS p ON p.Name = v.PipelineName
INNER JOIN cicd.BuildRun AS r ON r.PipelineId = p.PipelineId AND r.RunNumber = v.RunNumber
WHERE NOT EXISTS
(
    SELECT 1 FROM cicd.BuildRunDetail AS d WHERE d.BuildRunId = r.BuildRunId
);
GO

DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

INSERT INTO cicd.BuildStep (BuildRunId, StepOrder, Name, Status, StartedAt, FinishedAt, ExitCode, CreatedBy)
SELECT r.BuildRunId, v.StepOrder, v.Name, v.Status,
       DATEADD(MINUTE, -v.StartedMinutesAgo, @Now),
       DATEADD(MINUTE, -v.FinishedMinutesAgo, @Now),
       v.ExitCode,
       N'seed'
FROM
(
    VALUES
        (N'devhub-auth CI',    1, 1, N'Restore', 'Succeeded', 359,  358,  0),
        (N'devhub-auth CI',    1, 2, N'Build',   'Succeeded', 358,  355,  0),
        (N'devhub-auth CI',    1, 3, N'Test',    'Succeeded', 355,  353,  0),
        (N'devhub-auth CI',    1, 4, N'Pack',    'Succeeded', 353,  352,  0),
        (N'devhub-auth CI',    2, 1, N'Restore', 'Succeeded', 179,  178,  0),
        (N'devhub-auth CI',    2, 2, N'Build',   'Succeeded', 178,  176,  0),
        (N'devhub-auth CI',    2, 3, N'Test',    'Failed',    176,  175,  1),
        (N'devhub-auth CI',    2, 4, N'Pack',    'Skipped',   NULL, NULL, NULL),
        (N'devhub-storage CI', 1, 1, N'Restore', 'Pending',   NULL, NULL, NULL),
        (N'devhub-storage CI', 1, 2, N'Build',   'Pending',   NULL, NULL, NULL)
) AS v (PipelineName, RunNumber, StepOrder, Name, Status, StartedMinutesAgo, FinishedMinutesAgo, ExitCode)
INNER JOIN cicd.Pipeline AS p ON p.Name = v.PipelineName
INNER JOIN cicd.BuildRun AS r ON r.PipelineId = p.PipelineId AND r.RunNumber = v.RunNumber
WHERE NOT EXISTS
(
    SELECT 1 FROM cicd.BuildStep AS s WHERE s.BuildRunId = r.BuildRunId AND s.StepOrder = v.StepOrder
);
GO

DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

INSERT INTO cicd.RunStatusHistory (BuildRunId, FromStatus, ToStatus, Reason, CreatedAt, CreatedBy)
SELECT r.BuildRunId, v.FromStatus, v.ToStatus, v.Reason, DATEADD(MINUTE, -v.MinutesAgo, @Now), N'seed'
FROM
(
    VALUES
        (N'devhub-auth CI',    1, NULL,      'Queued',    N'Queued by Push trigger',        360),
        (N'devhub-auth CI',    1, 'Queued',  'Running',   N'Leased agent build-win-01',     359),
        (N'devhub-auth CI',    1, 'Running', 'Succeeded', N'Completed with 0 failed step(s)', 352),
        (N'devhub-auth CI',    2, NULL,      'Queued',    N'Queued by PullRequest trigger', 180),
        (N'devhub-auth CI',    2, 'Queued',  'Running',   N'Leased agent build-win-01',     179),
        (N'devhub-auth CI',    2, 'Running', 'Failed',    N'Completed with 1 failed step(s)', 175),
        (N'devhub-storage CI', 1, NULL,      'Queued',    N'Queued by Schedule trigger',      4),
        (N'billing-sdk CI',    1, NULL,      'Queued',    N'Queued by Manual trigger',       90),
        (N'billing-sdk CI',    1, 'Queued',  'Cancelled', N'Superseded by a newer commit',   85)
) AS v (PipelineName, RunNumber, FromStatus, ToStatus, Reason, MinutesAgo)
INNER JOIN cicd.Pipeline AS p ON p.Name = v.PipelineName
INNER JOIN cicd.BuildRun AS r ON r.PipelineId = p.PipelineId AND r.RunNumber = v.RunNumber
WHERE NOT EXISTS
(
    SELECT 1
    FROM cicd.RunStatusHistory AS h
    WHERE h.BuildRunId = r.BuildRunId
      AND h.ToStatus = v.ToStatus
      AND ISNULL(h.FromStatus, '') = ISNULL(v.FromStatus, '')
);
GO

INSERT INTO cicd.ArtifactUsage (BuildRunId, ArtifactId, UsageKind, CreatedBy)
SELECT r.BuildRunId, a.ArtifactId, v.UsageKind, N'seed'
FROM
(
    VALUES
        (N'devhub-auth CI', 1, '3b1f9c2a7d4e8f0b6c5a9e2d7f1b4c8a0e3d6f9b2c5a8e1d4f7b0c3a6e9d2f5b', 'Produced'),
        (N'devhub-auth CI', 1, 'c4a7e0d3b6f9c2e5a8d1b4f7e0a3d6c9b2e5f8a1d4c7b0e3f6a9d2c5b8e1f4a7', 'Consumed'),
        (N'devhub-auth CI', 2, 'c4a7e0d3b6f9c2e5a8d1b4f7e0a3d6c9b2e5f8a1d4c7b0e3f6a9d2c5b8e1f4a7', 'Consumed')
) AS v (PipelineName, RunNumber, Sha256, UsageKind)
INNER JOIN cicd.Pipeline AS p ON p.Name = v.PipelineName
INNER JOIN cicd.BuildRun AS r ON r.PipelineId = p.PipelineId AND r.RunNumber = v.RunNumber
INNER JOIN cicd.Artifact AS a ON a.Sha256 = v.Sha256
WHERE NOT EXISTS
(
    SELECT 1
    FROM cicd.ArtifactUsage AS u
    WHERE u.BuildRunId = r.BuildRunId
      AND u.ArtifactId = a.ArtifactId
      AND u.UsageKind = v.UsageKind
);
GO
