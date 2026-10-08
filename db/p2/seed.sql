USE DevHub_Registry;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

INSERT INTO registry.Publisher (Slug, DisplayName, ContactEmail, IsVerified, CreatedBy)
SELECT v.Slug, v.DisplayName, v.ContactEmail, v.IsVerified, N'seed'
FROM
(
    VALUES
        (N'platform-team', N'Platform Engineering', N'platform@devhub.local', 1),
        (N'payments-team', N'Payments Engineering', N'payments@devhub.local', 1),
        (N'research-team', N'Innovation Research',  N'research@devhub.local', 0)
) AS v (Slug, DisplayName, ContactEmail, IsVerified)
WHERE NOT EXISTS
(
    SELECT 1 FROM registry.Publisher AS p WHERE p.Slug = v.Slug
);
GO

INSERT INTO registry.Tag (Name, Description, CreatedBy)
SELECT v.Name, v.Description, N'seed'
FROM
(
    VALUES
        (N'authentication', N'Identity, tokens and access control'),
        (N'storage',        N'Blob, file and object storage'),
        (N'payments',       N'Billing, invoicing and settlement'),
        (N'infrastructure', N'Cross-cutting platform concerns'),
        (N'experimental',   N'Not recommended for production use')
) AS v (Name, Description)
WHERE NOT EXISTS
(
    SELECT 1 FROM registry.Tag AS t WHERE t.Name = v.Name
);
GO

INSERT INTO registry.Package (PublisherId, Name, Description, ProjectUrl, DownloadCount, CreatedBy)
SELECT pub.PublisherId, v.Name, v.Description, v.ProjectUrl, v.DownloadCount, N'seed'
FROM
(
    VALUES
        (N'platform-team', N'devhub-auth',    N'Token issuance and claim validation for DevHub services.', N'https://git.devhub.local/platform-team/devhub-auth',    48210),
        (N'platform-team', N'devhub-storage', N'Object storage abstraction over S3 and Azure Blob.',       N'https://git.devhub.local/platform-team/devhub-storage', 31544),
        (N'payments-team', N'billing-sdk',    N'Client for the internal billing and invoicing API.',       N'https://git.devhub.local/payments-team/billing-sdk',     6120),
        (N'research-team', N'graph-explorer', N'Experimental dependency graph visualiser.',                NULL,                                                         87)
) AS v (PublisherSlug, Name, Description, ProjectUrl, DownloadCount)
INNER JOIN registry.Publisher AS pub ON pub.Slug = v.PublisherSlug
WHERE NOT EXISTS
(
    SELECT 1 FROM registry.Package AS p WHERE p.PublisherId = pub.PublisherId AND p.Name = v.Name
)
ORDER BY pub.Slug, v.Name;
GO

DECLARE @Now DATETIME2(3) = SYSUTCDATETIME();

INSERT INTO registry.PackageVersion (PackageId, SemVer, MajorVersion, MinorVersion, PatchVersion, PreReleaseTag, SizeBytes, Sha256, IsDeprecated, PublishedAt, CreatedBy)
SELECT p.PackageId, v.SemVer, v.MajorVersion, v.MinorVersion, v.PatchVersion, v.PreReleaseTag, v.SizeBytes, v.Sha256, v.IsDeprecated, DATEADD(DAY, -v.DaysAgo, @Now), N'seed'
FROM
(
    VALUES
        (N'platform-team', N'devhub-auth',    N'1.9.0',       1, 9, 0, NULL,     987136, '0a1b2c3d4e5f60718293a4b5c6d7e8f90a1b2c3d4e5f60718293a4b5c6d7e8f9', 1, 180),
        (N'platform-team', N'devhub-auth',    N'2.0.0',       2, 0, 0, NULL,    1048576, '3b1f9c2a7d4e8f0b6c5a9e2d7f1b4c8a0e3d6f9b2c5a8e1d4f7b0c3a6e9d2f5b', 0,  60),
        (N'platform-team', N'devhub-auth',    N'2.1.0',       2, 1, 0, NULL,    1130496, '9f2e5d8c1b4a7e0d3c6f9b2e5a8d1c4f7b0e3a6d9c2f5b8e1a4d7c0f3b6e9a2d', 0,   7),
        (N'platform-team', N'devhub-storage', N'1.4.2',       1, 4, 2, NULL,    2097152, 'c4a7e0d3b6f9c2e5a8d1b4f7e0a3d6c9b2e5f8a1d4c7b0e3f6a9d2c5b8e1f4a7', 0,  21),
        (N'payments-team', N'billing-sdk',    N'0.9.1',       0, 9, 1, NULL,     524288, 'e1d4c7b0a3f6e9d2c5b8a1f4e7d0c3b6a9f2e5d8c1b4a7f0e3d6c9b2a5f8e1d4', 0,  14),
        (N'research-team', N'graph-explorer', N'0.1.0-alpha', 0, 1, 0, N'alpha', 262144, 'f5a8d1c4b7e0a3f6d9c2b5e8a1f4d7c0b3e6a9f2d5c8b1e4a7f0d3c6b9e2a5f8', 0,   3)
) AS v (PublisherSlug, PackageName, SemVer, MajorVersion, MinorVersion, PatchVersion, PreReleaseTag, SizeBytes, Sha256, IsDeprecated, DaysAgo)
INNER JOIN registry.Publisher AS pub ON pub.Slug = v.PublisherSlug
INNER JOIN registry.Package AS p ON p.PublisherId = pub.PublisherId AND p.Name = v.PackageName
WHERE NOT EXISTS
(
    SELECT 1 FROM registry.PackageVersion AS pv WHERE pv.PackageId = p.PackageId AND pv.SemVer = v.SemVer
)
ORDER BY v.DaysAgo DESC;
GO

INSERT INTO registry.PackageManifest (PackageVersionId, ReadmeMarkdown, LicenseExpression, TargetFramework, EntryPoints, CreatedBy)
SELECT
    pv.PackageVersionId,
    CONCAT(N'# ', p.Name, N' ', pv.SemVer, NCHAR(10), NCHAR(10), p.Description),
    N'MIT',
    CASE WHEN p.Name = N'graph-explorer' THEN N'net9.0' ELSE N'net8.0' END,
    CONCAT(N'{"lib":["lib/', p.Name, N'.dll"]}'),
    N'seed'
FROM registry.PackageVersion AS pv
INNER JOIN registry.Package AS p ON p.PackageId = pv.PackageId
WHERE pv.CreatedBy = N'seed'
  AND NOT EXISTS
  (
      SELECT 1 FROM registry.PackageManifest AS m WHERE m.PackageVersionId = pv.PackageVersionId
  );
GO

INSERT INTO registry.PackageTag (PackageId, TagId, AssignedAt, CreatedBy)
SELECT p.PackageId, t.TagId, DATEADD(DAY, -v.DaysAgo, SYSUTCDATETIME()), N'seed'
FROM
(
    VALUES
        (N'devhub-auth',    N'authentication', 170),
        (N'devhub-auth',    N'infrastructure', 120),
        (N'devhub-storage', N'storage',         90),
        (N'devhub-storage', N'infrastructure',  90),
        (N'billing-sdk',    N'payments',        30),
        (N'graph-explorer', N'experimental',     3),
        (N'graph-explorer', N'infrastructure',   2)
) AS v (PackageName, TagName, DaysAgo)
INNER JOIN registry.Package AS p ON p.Name = v.PackageName
INNER JOIN registry.Tag AS t ON t.Name = v.TagName
WHERE NOT EXISTS
(
    SELECT 1 FROM registry.PackageTag AS pt WHERE pt.PackageId = p.PackageId AND pt.TagId = t.TagId
);
GO
