IF DB_ID(N'DevHub_Registry') IS NULL
BEGIN
    CREATE DATABASE DevHub_Registry;
END;
GO

USE DevHub_Registry;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
GO

IF SCHEMA_ID(N'registry') IS NULL
BEGIN
    EXEC(N'CREATE SCHEMA registry AUTHORIZATION dbo;');
END;
GO

IF OBJECT_ID(N'registry.Publisher', N'U') IS NULL
BEGIN
    CREATE TABLE registry.Publisher
    (
        PublisherId     BIGINT          IDENTITY(1, 1) NOT NULL,
        Slug            NVARCHAR(100)   NOT NULL,
        DisplayName     NVARCHAR(200)   NOT NULL,
        ContactEmail    NVARCHAR(256)   NOT NULL,
        IsVerified      BIT             NOT NULL CONSTRAINT DF_Publisher_IsVerified DEFAULT (0),
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_Publisher_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_Publisher_CreatedBy DEFAULT (SUSER_SNAME()),
        UpdatedAt       DATETIME2(3)    NULL,
        UpdatedBy       NVARCHAR(128)   NULL,
        IsDeleted       BIT             NOT NULL CONSTRAINT DF_Publisher_IsDeleted DEFAULT (0),
        RowVersion      ROWVERSION      NOT NULL,

        CONSTRAINT PK_Publisher PRIMARY KEY CLUSTERED (PublisherId),
        CONSTRAINT UQ_Publisher_Slug UNIQUE (Slug),
        CONSTRAINT UQ_Publisher_ContactEmail UNIQUE (ContactEmail),
        CONSTRAINT CK_Publisher_Slug_Format CHECK (LEN(Slug) >= 2 AND Slug NOT LIKE N'%[^a-z0-9-]%'),
        CONSTRAINT CK_Publisher_ContactEmail CHECK (ContactEmail LIKE N'%_@_%._%')
    );
END;
GO

IF OBJECT_ID(N'registry.Package', N'U') IS NULL
BEGIN
    CREATE TABLE registry.Package
    (
        PackageId       BIGINT          IDENTITY(1, 1) NOT NULL,
        PublisherId     BIGINT          NOT NULL,
        Name            NVARCHAR(200)   NOT NULL,
        Description     NVARCHAR(2000)  NULL,
        ProjectUrl      NVARCHAR(400)   NULL,
        DownloadCount   BIGINT          NOT NULL CONSTRAINT DF_Package_DownloadCount DEFAULT (0),
        IsListed        BIT             NOT NULL CONSTRAINT DF_Package_IsListed DEFAULT (1),
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_Package_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_Package_CreatedBy DEFAULT (SUSER_SNAME()),
        UpdatedAt       DATETIME2(3)    NULL,
        UpdatedBy       NVARCHAR(128)   NULL,
        IsDeleted       BIT             NOT NULL CONSTRAINT DF_Package_IsDeleted DEFAULT (0),
        RowVersion      ROWVERSION      NOT NULL,

        CONSTRAINT PK_Package PRIMARY KEY CLUSTERED (PackageId),
        CONSTRAINT FK_Package_Publisher FOREIGN KEY (PublisherId) REFERENCES registry.Publisher (PublisherId),
        CONSTRAINT UQ_Package_Publisher_Name UNIQUE (PublisherId, Name),
        CONSTRAINT CK_Package_Name_Format CHECK (LEN(Name) >= 2 AND Name NOT LIKE N'%[^a-zA-Z0-9._-]%'),
        CONSTRAINT CK_Package_DownloadCount_NonNegative CHECK (DownloadCount >= 0),
        CONSTRAINT CK_Package_ProjectUrl_Scheme CHECK (ProjectUrl IS NULL OR ProjectUrl LIKE N'https://%')
    );
END;
GO

IF OBJECT_ID(N'registry.PackageVersion', N'U') IS NULL
BEGIN
    CREATE TABLE registry.PackageVersion
    (
        PackageVersionId BIGINT         IDENTITY(1, 1) NOT NULL,
        PackageId       BIGINT          NOT NULL,
        SemVer          NVARCHAR(50)    NOT NULL,
        MajorVersion    INT             NOT NULL,
        MinorVersion    INT             NOT NULL,
        PatchVersion    INT             NOT NULL,
        PreReleaseTag   NVARCHAR(50)    NULL,
        SizeBytes       BIGINT          NOT NULL,
        Sha256          CHAR(64)        NOT NULL,
        IsDeprecated    BIT             NOT NULL CONSTRAINT DF_PackageVersion_IsDeprecated DEFAULT (0),
        PublishedAt     DATETIME2(3)    NOT NULL CONSTRAINT DF_PackageVersion_PublishedAt DEFAULT (SYSUTCDATETIME()),
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_PackageVersion_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_PackageVersion_CreatedBy DEFAULT (SUSER_SNAME()),
        UpdatedAt       DATETIME2(3)    NULL,
        UpdatedBy       NVARCHAR(128)   NULL,
        IsDeleted       BIT             NOT NULL CONSTRAINT DF_PackageVersion_IsDeleted DEFAULT (0),
        RowVersion      ROWVERSION      NOT NULL,

        CONSTRAINT PK_PackageVersion PRIMARY KEY CLUSTERED (PackageVersionId),
        CONSTRAINT FK_PackageVersion_Package FOREIGN KEY (PackageId) REFERENCES registry.Package (PackageId) ON DELETE CASCADE,
        CONSTRAINT UQ_PackageVersion_Package_SemVer UNIQUE (PackageId, SemVer),
        CONSTRAINT UQ_PackageVersion_Sha256 UNIQUE (Sha256),
        CONSTRAINT CK_PackageVersion_SizeBytes_Positive CHECK (SizeBytes > 0),
        CONSTRAINT CK_PackageVersion_Numbers_NonNegative CHECK (MajorVersion >= 0 AND MinorVersion >= 0 AND PatchVersion >= 0),
        CONSTRAINT CK_PackageVersion_SemVer_MatchesParts CHECK
        (
            SemVer = CONCAT(MajorVersion, N'.', MinorVersion, N'.', PatchVersion, CASE WHEN PreReleaseTag IS NULL THEN N'' ELSE CONCAT(N'-', PreReleaseTag) END)
        ),
        CONSTRAINT CK_PackageVersion_Sha256_Hex CHECK (LEN(Sha256) = 64 AND Sha256 NOT LIKE '%[^0-9a-f]%')
    );
END;
GO

IF OBJECT_ID(N'registry.PackageManifest', N'U') IS NULL
BEGIN
    CREATE TABLE registry.PackageManifest
    (
        PackageVersionId  BIGINT         NOT NULL,
        ReadmeMarkdown    NVARCHAR(MAX)  NULL,
        LicenseExpression NVARCHAR(100)  NOT NULL,
        TargetFramework   NVARCHAR(50)   NOT NULL,
        EntryPoints       NVARCHAR(MAX)  NULL,
        CreatedAt         DATETIME2(3)   NOT NULL CONSTRAINT DF_PackageManifest_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy         NVARCHAR(128)  NOT NULL CONSTRAINT DF_PackageManifest_CreatedBy DEFAULT (SUSER_SNAME()),
        UpdatedAt         DATETIME2(3)   NULL,
        UpdatedBy         NVARCHAR(128)  NULL,
        IsDeleted         BIT            NOT NULL CONSTRAINT DF_PackageManifest_IsDeleted DEFAULT (0),

        CONSTRAINT PK_PackageManifest PRIMARY KEY CLUSTERED (PackageVersionId),
        CONSTRAINT FK_PackageManifest_PackageVersion FOREIGN KEY (PackageVersionId) REFERENCES registry.PackageVersion (PackageVersionId) ON DELETE CASCADE,
        CONSTRAINT CK_PackageManifest_TargetFramework CHECK (TargetFramework LIKE N'net[0-9]%'),
        CONSTRAINT CK_PackageManifest_EntryPoints_Json CHECK (EntryPoints IS NULL OR ISJSON(EntryPoints) = 1)
    );
END;
GO

IF OBJECT_ID(N'registry.Tag', N'U') IS NULL
BEGIN
    CREATE TABLE registry.Tag
    (
        TagId           BIGINT          IDENTITY(1, 1) NOT NULL,
        Name            NVARCHAR(50)    NOT NULL,
        Description     NVARCHAR(400)   NULL,
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_Tag_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_Tag_CreatedBy DEFAULT (SUSER_SNAME()),
        UpdatedAt       DATETIME2(3)    NULL,
        UpdatedBy       NVARCHAR(128)   NULL,
        IsDeleted       BIT             NOT NULL CONSTRAINT DF_Tag_IsDeleted DEFAULT (0),

        CONSTRAINT PK_Tag PRIMARY KEY CLUSTERED (TagId),
        CONSTRAINT UQ_Tag_Name UNIQUE (Name),
        CONSTRAINT CK_Tag_Name_Format CHECK (LEN(Name) >= 2 AND Name NOT LIKE N'%[^a-z0-9-]%')
    );
END;
GO

IF OBJECT_ID(N'registry.PackageTag', N'U') IS NULL
BEGIN
    CREATE TABLE registry.PackageTag
    (
        PackageId       BIGINT          NOT NULL,
        TagId           BIGINT          NOT NULL,
        AssignedAt      DATETIME2(3)    NOT NULL CONSTRAINT DF_PackageTag_AssignedAt DEFAULT (SYSUTCDATETIME()),
        CreatedAt       DATETIME2(3)    NOT NULL CONSTRAINT DF_PackageTag_CreatedAt DEFAULT (SYSUTCDATETIME()),
        CreatedBy       NVARCHAR(128)   NOT NULL CONSTRAINT DF_PackageTag_CreatedBy DEFAULT (SUSER_SNAME()),

        CONSTRAINT PK_PackageTag PRIMARY KEY CLUSTERED (PackageId, TagId),
        CONSTRAINT FK_PackageTag_Package FOREIGN KEY (PackageId) REFERENCES registry.Package (PackageId) ON DELETE CASCADE,
        CONSTRAINT FK_PackageTag_Tag FOREIGN KEY (TagId) REFERENCES registry.Tag (TagId) ON DELETE CASCADE
    );
END;
GO

IF INDEXPROPERTY(OBJECT_ID(N'registry.Package'), N'IX_Package_PublisherId_Name', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_Package_PublisherId_Name
        ON registry.Package (PublisherId, Name)
        INCLUDE (Description, DownloadCount)
        WHERE IsDeleted = 0 AND IsListed = 1;
GO

IF INDEXPROPERTY(OBJECT_ID(N'registry.Package'), N'IX_Package_DownloadCount', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_Package_DownloadCount
        ON registry.Package (DownloadCount DESC)
        INCLUDE (Name, PublisherId)
        WHERE IsDeleted = 0 AND IsListed = 1;
GO

IF INDEXPROPERTY(OBJECT_ID(N'registry.PackageVersion'), N'IX_PackageVersion_Package_PublishedAt', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_PackageVersion_Package_PublishedAt
        ON registry.PackageVersion (PackageId, PublishedAt DESC)
        INCLUDE (SemVer, SizeBytes, IsDeprecated)
        WHERE IsDeleted = 0;
GO

IF INDEXPROPERTY(OBJECT_ID(N'registry.PackageVersion'), N'IX_PackageVersion_Ordering', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_PackageVersion_Ordering
        ON registry.PackageVersion (PackageId, MajorVersion DESC, MinorVersion DESC, PatchVersion DESC)
        INCLUDE (SemVer, PublishedAt, SizeBytes)
        WHERE IsDeleted = 0 AND IsDeprecated = 0;
GO

IF INDEXPROPERTY(OBJECT_ID(N'registry.PackageTag'), N'IX_PackageTag_TagId_AssignedAt', N'IndexID') IS NULL
    CREATE NONCLUSTERED INDEX IX_PackageTag_TagId_AssignedAt
        ON registry.PackageTag (TagId, AssignedAt DESC)
        INCLUDE (PackageId);
GO

CREATE OR ALTER VIEW registry.vw_LatestPackageVersion
AS
SELECT
    p.PackageId,
    pub.Slug AS PublisherSlug,
    p.Name AS PackageName,
    CONCAT(pub.Slug, N'/', p.Name) AS PackageKey,
    v.PackageVersionId,
    v.SemVer,
    v.PublishedAt,
    v.SizeBytes,
    m.LicenseExpression,
    m.TargetFramework
FROM registry.Package AS p
INNER JOIN registry.Publisher AS pub ON pub.PublisherId = p.PublisherId
CROSS APPLY
(
    SELECT TOP (1) pv.PackageVersionId, pv.SemVer, pv.PublishedAt, pv.SizeBytes
    FROM registry.PackageVersion AS pv
    WHERE pv.PackageId = p.PackageId
      AND pv.IsDeleted = 0
      AND pv.IsDeprecated = 0
    ORDER BY pv.MajorVersion DESC, pv.MinorVersion DESC, pv.PatchVersion DESC
) AS v
LEFT JOIN registry.PackageManifest AS m ON m.PackageVersionId = v.PackageVersionId
WHERE p.IsDeleted = 0
  AND p.IsListed = 1;
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
    ('TABLE', N'Publisher',       NULL,         NULL,                                     N'Team or organisation that owns packages. Reference data.'),
    ('TABLE', N'Package',         NULL,         NULL,                                     N'Published library. Owner of the package name copied into CI/CD and Telemetry.'),
    ('TABLE', N'PackageVersion',  NULL,         NULL,                                     N'Immutable published version of a package.'),
    ('TABLE', N'PackageManifest', NULL,         NULL,                                     N'1:1 extension of PackageVersion: readme, licence, framework, entry points.'),
    ('TABLE', N'Tag',             NULL,         NULL,                                     N'Shared classification vocabulary. Reference data.'),
    ('TABLE', N'PackageTag',      NULL,         NULL,                                     N'M:N junction entity between Package and Tag with its own field AssignedAt.'),
    ('TABLE', N'Package',         'CONSTRAINT', N'FK_Package_Publisher',                  N'1:N Publisher -> Package. No cascade: a publisher with packages is never removed.'),
    ('TABLE', N'PackageVersion',  'CONSTRAINT', N'FK_PackageVersion_Package',             N'1:N Package -> PackageVersion. Cascade: versions cannot exist without their package.'),
    ('TABLE', N'PackageManifest', 'CONSTRAINT', N'FK_PackageManifest_PackageVersion',     N'1:1 PackageVersion <-> PackageManifest. The FK is also the PK of the dependent table.'),
    ('TABLE', N'PackageTag',      'CONSTRAINT', N'FK_PackageTag_Package',                 N'M:N Package <-> Tag, package side.'),
    ('TABLE', N'PackageTag',      'CONSTRAINT', N'FK_PackageTag_Tag',                     N'M:N Package <-> Tag, tag side.'),
    ('TABLE', N'PackageTag',      'COLUMN',     N'AssignedAt',                            N'Own field of the M:N junction: when the tag was assigned to the package.'),
    ('TABLE', N'Package',         'INDEX',      N'IX_Package_PublisherId_Name',           N'Query: list one publisher''s listed packages by name; also resolves a packageKey to a package.'),
    ('TABLE', N'Package',         'INDEX',      N'IX_Package_DownloadCount',              N'Query: most downloaded listed packages, top-N.'),
    ('TABLE', N'PackageVersion',  'INDEX',      N'IX_PackageVersion_Package_PublishedAt', N'Query: version history of a package, newest published first.'),
    ('TABLE', N'PackageVersion',  'INDEX',      N'IX_PackageVersion_Ordering',            N'Query: latest non-deprecated version by semantic order, used by vw_LatestPackageVersion.'),
    ('TABLE', N'PackageTag',      'INDEX',      N'IX_PackageTag_TagId_AssignedAt',        N'Query: packages with a tag, most recently tagged first. The PK only serves package -> tags.'),
    ('VIEW',  N'vw_LatestPackageVersion', NULL, NULL,                                     N'Denormalised read model: each listed package with its latest version, licence and framework.');

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
        FROM sys.fn_listextendedproperty(N'MS_Description', N'SCHEMA', N'registry', @ObjectKind, @ObjectName, @ChildKind, @ChildName)
    )
        EXEC sys.sp_updateextendedproperty
            @name = N'MS_Description', @value = @Text,
            @level0type = N'SCHEMA', @level0name = N'registry',
            @level1type = @ObjectKind, @level1name = @ObjectName,
            @level2type = @ChildKind, @level2name = @ChildName;
    ELSE
        EXEC sys.sp_addextendedproperty
            @name = N'MS_Description', @value = @Text,
            @level0type = N'SCHEMA', @level0name = N'registry',
            @level1type = @ObjectKind, @level1name = @ObjectName,
            @level2type = @ChildKind, @level2name = @ChildName;

    FETCH NEXT FROM description_cursor INTO @ObjectKind, @ObjectName, @ChildKind, @ChildName, @Text;
END;

CLOSE description_cursor;
DEALLOCATE description_cursor;
GO
