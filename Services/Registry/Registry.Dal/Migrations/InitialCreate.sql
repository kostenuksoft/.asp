IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF SCHEMA_ID(N'registry') IS NULL EXEC(N'CREATE SCHEMA [registry];');

CREATE TABLE [registry].[Publisher] (
    [PublisherId] bigint NOT NULL IDENTITY,
    [Slug] nvarchar(100) NOT NULL,
    [DisplayName] nvarchar(200) NOT NULL,
    [ContactEmail] nvarchar(256) NOT NULL,
    [IsVerified] bit NOT NULL DEFAULT CAST(0 AS bit),
    [RowVersion] rowversion NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(128) NOT NULL DEFAULT (SUSER_SNAME()),
    [UpdatedAt] datetime2(3) NULL,
    [UpdatedBy] nvarchar(128) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_Publisher] PRIMARY KEY ([PublisherId]),
    CONSTRAINT [CK_Publisher_ContactEmail] CHECK ([ContactEmail] LIKE N'%_@_%._%'),
    CONSTRAINT [CK_Publisher_Slug_Format] CHECK (LEN([Slug]) >= 2 AND [Slug] NOT LIKE N'%[^a-z0-9-]%')
);

CREATE TABLE [registry].[Tag] (
    [TagId] bigint NOT NULL IDENTITY,
    [Name] nvarchar(50) NOT NULL,
    [Description] nvarchar(400) NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(128) NOT NULL DEFAULT (SUSER_SNAME()),
    [UpdatedAt] datetime2(3) NULL,
    [UpdatedBy] nvarchar(128) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_Tag] PRIMARY KEY ([TagId]),
    CONSTRAINT [CK_Tag_Name_Format] CHECK (LEN([Name]) >= 2 AND [Name] NOT LIKE N'%[^a-z0-9-]%')
);

CREATE TABLE [registry].[Package] (
    [PackageId] bigint NOT NULL IDENTITY,
    [PublisherId] bigint NOT NULL,
    [Name] nvarchar(200) NOT NULL,
    [Description] nvarchar(2000) NULL,
    [ProjectUrl] nvarchar(400) NULL,
    [LicensePrice] decimal(18,2) NOT NULL DEFAULT 0.0,
    [DownloadCount] bigint NOT NULL DEFAULT CAST(0 AS bigint),
    [IsListed] bit NOT NULL,
    [RowVersion] rowversion NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(128) NOT NULL DEFAULT (SUSER_SNAME()),
    [UpdatedAt] datetime2(3) NULL,
    [UpdatedBy] nvarchar(128) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_Package] PRIMARY KEY ([PackageId]),
    CONSTRAINT [CK_Package_DownloadCount_NonNegative] CHECK ([DownloadCount] >= 0),
    CONSTRAINT [CK_Package_LicensePrice_NonNegative] CHECK ([LicensePrice] >= 0),
    CONSTRAINT [CK_Package_Name_Format] CHECK (LEN([Name]) >= 2 AND [Name] NOT LIKE N'%[^a-zA-Z0-9._-]%'),
    CONSTRAINT [CK_Package_ProjectUrl_Scheme] CHECK ([ProjectUrl] IS NULL OR [ProjectUrl] LIKE N'https://%'),
    CONSTRAINT [FK_Package_Publisher] FOREIGN KEY ([PublisherId]) REFERENCES [registry].[Publisher] ([PublisherId])
);

CREATE TABLE [registry].[PackageTag] (
    [PackageId] bigint NOT NULL,
    [TagId] bigint NOT NULL,
    [AssignedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(128) NOT NULL DEFAULT (SUSER_SNAME()),
    CONSTRAINT [PK_PackageTag] PRIMARY KEY ([PackageId], [TagId]),
    CONSTRAINT [FK_PackageTag_Package] FOREIGN KEY ([PackageId]) REFERENCES [registry].[Package] ([PackageId]) ON DELETE CASCADE,
    CONSTRAINT [FK_PackageTag_Tag] FOREIGN KEY ([TagId]) REFERENCES [registry].[Tag] ([TagId]) ON DELETE CASCADE
);

CREATE TABLE [registry].[PackageVersion] (
    [PackageVersionId] bigint NOT NULL IDENTITY,
    [PackageId] bigint NOT NULL,
    [SemVer] nvarchar(50) NOT NULL,
    [MajorVersion] int NOT NULL,
    [MinorVersion] int NOT NULL,
    [PatchVersion] int NOT NULL,
    [PreReleaseTag] nvarchar(50) NULL,
    [SizeBytes] bigint NOT NULL,
    [Sha256] char(64) NOT NULL,
    [IsDeprecated] bit NOT NULL DEFAULT CAST(0 AS bit),
    [PublishedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [RowVersion] rowversion NOT NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(128) NOT NULL DEFAULT (SUSER_SNAME()),
    [UpdatedAt] datetime2(3) NULL,
    [UpdatedBy] nvarchar(128) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_PackageVersion] PRIMARY KEY ([PackageVersionId]),
    CONSTRAINT [CK_PackageVersion_Numbers_NonNegative] CHECK ([MajorVersion] >= 0 AND [MinorVersion] >= 0 AND [PatchVersion] >= 0),
    CONSTRAINT [CK_PackageVersion_SemVer_MatchesParts] CHECK ([SemVer] = CONCAT([MajorVersion], N'.', [MinorVersion], N'.', [PatchVersion], CASE WHEN [PreReleaseTag] IS NULL THEN N'' ELSE CONCAT(N'-', [PreReleaseTag]) END)),
    CONSTRAINT [CK_PackageVersion_Sha256_Hex] CHECK (LEN([Sha256]) = 64 AND [Sha256] NOT LIKE '%[^0-9a-f]%'),
    CONSTRAINT [CK_PackageVersion_SizeBytes_Positive] CHECK ([SizeBytes] > 0),
    CONSTRAINT [FK_PackageVersion_Package] FOREIGN KEY ([PackageId]) REFERENCES [registry].[Package] ([PackageId]) ON DELETE CASCADE
);

CREATE TABLE [registry].[PackageManifest] (
    [PackageVersionId] bigint NOT NULL,
    [ReadmeMarkdown] nvarchar(max) NULL,
    [LicenseExpression] nvarchar(100) NOT NULL,
    [TargetFramework] nvarchar(50) NOT NULL,
    [EntryPoints] nvarchar(max) NULL,
    [CreatedAt] datetime2(3) NOT NULL DEFAULT (SYSUTCDATETIME()),
    [CreatedBy] nvarchar(128) NOT NULL DEFAULT (SUSER_SNAME()),
    [UpdatedAt] datetime2(3) NULL,
    [UpdatedBy] nvarchar(128) NULL,
    [IsDeleted] bit NOT NULL DEFAULT CAST(0 AS bit),
    CONSTRAINT [PK_PackageManifest] PRIMARY KEY ([PackageVersionId]),
    CONSTRAINT [CK_PackageManifest_EntryPoints_Json] CHECK ([EntryPoints] IS NULL OR ISJSON([EntryPoints]) = 1),
    CONSTRAINT [CK_PackageManifest_TargetFramework] CHECK ([TargetFramework] LIKE N'net[0-9]%'),
    CONSTRAINT [FK_PackageManifest_PackageVersion] FOREIGN KEY ([PackageVersionId]) REFERENCES [registry].[PackageVersion] ([PackageVersionId]) ON DELETE CASCADE
);

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PublisherId', N'ContactEmail', N'CreatedAt', N'CreatedBy', N'DisplayName', N'IsVerified', N'Slug', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[registry].[Publisher]'))
    SET IDENTITY_INSERT [registry].[Publisher] ON;
INSERT INTO [registry].[Publisher] ([PublisherId], [ContactEmail], [CreatedAt], [CreatedBy], [DisplayName], [IsVerified], [Slug], [UpdatedAt], [UpdatedBy])
VALUES (CAST(1 AS bigint), N'platform@devhub.local', '2026-09-01T00:00:00.000Z', N'seed', N'Platform Engineering', CAST(1 AS bit), N'platform-team', NULL, NULL),
(CAST(2 AS bigint), N'payments@devhub.local', '2026-09-01T00:00:00.000Z', N'seed', N'Payments Engineering', CAST(1 AS bit), N'payments-team', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PublisherId', N'ContactEmail', N'CreatedAt', N'CreatedBy', N'DisplayName', N'IsVerified', N'Slug', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[registry].[Publisher]'))
    SET IDENTITY_INSERT [registry].[Publisher] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PublisherId', N'ContactEmail', N'CreatedAt', N'CreatedBy', N'DisplayName', N'Slug', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[registry].[Publisher]'))
    SET IDENTITY_INSERT [registry].[Publisher] ON;
INSERT INTO [registry].[Publisher] ([PublisherId], [ContactEmail], [CreatedAt], [CreatedBy], [DisplayName], [Slug], [UpdatedAt], [UpdatedBy])
VALUES (CAST(3 AS bigint), N'research@devhub.local', '2026-09-01T00:00:00.000Z', N'seed', N'Innovation Research', N'research-team', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'PublisherId', N'ContactEmail', N'CreatedAt', N'CreatedBy', N'DisplayName', N'Slug', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[registry].[Publisher]'))
    SET IDENTITY_INSERT [registry].[Publisher] OFF;

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'TagId', N'CreatedAt', N'CreatedBy', N'Description', N'Name', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[registry].[Tag]'))
    SET IDENTITY_INSERT [registry].[Tag] ON;
INSERT INTO [registry].[Tag] ([TagId], [CreatedAt], [CreatedBy], [Description], [Name], [UpdatedAt], [UpdatedBy])
VALUES (CAST(1 AS bigint), '2026-09-01T00:00:00.000Z', N'seed', N'Identity, tokens and access control', N'authentication', NULL, NULL),
(CAST(2 AS bigint), '2026-09-01T00:00:00.000Z', N'seed', N'Blob, file and object storage', N'storage', NULL, NULL),
(CAST(3 AS bigint), '2026-09-01T00:00:00.000Z', N'seed', N'Billing, invoicing and settlement', N'payments', NULL, NULL),
(CAST(4 AS bigint), '2026-09-01T00:00:00.000Z', N'seed', N'Cross-cutting platform concerns', N'infrastructure', NULL, NULL),
(CAST(5 AS bigint), '2026-09-01T00:00:00.000Z', N'seed', N'Not recommended for production use', N'experimental', NULL, NULL);
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'TagId', N'CreatedAt', N'CreatedBy', N'Description', N'Name', N'UpdatedAt', N'UpdatedBy') AND [object_id] = OBJECT_ID(N'[registry].[Tag]'))
    SET IDENTITY_INSERT [registry].[Tag] OFF;

CREATE INDEX [IX_Package_DownloadCount] ON [registry].[Package] ([DownloadCount] DESC) INCLUDE ([Name], [PublisherId]) WHERE [IsDeleted] = 0 AND [IsListed] = 1;

CREATE INDEX [IX_Package_PublisherId_Name] ON [registry].[Package] ([PublisherId], [Name]) INCLUDE ([Description], [DownloadCount]) WHERE [IsDeleted] = 0 AND [IsListed] = 1;

CREATE UNIQUE INDEX [UQ_Package_Publisher_Name] ON [registry].[Package] ([PublisherId], [Name]);

CREATE INDEX [IX_PackageTag_TagId_AssignedAt] ON [registry].[PackageTag] ([TagId], [AssignedAt] DESC) INCLUDE ([PackageId]);

CREATE INDEX [IX_PackageVersion_Ordering] ON [registry].[PackageVersion] ([PackageId], [MajorVersion] DESC, [MinorVersion] DESC, [PatchVersion] DESC) INCLUDE ([SemVer], [PublishedAt], [SizeBytes]) WHERE [IsDeleted] = 0 AND [IsDeprecated] = 0;

CREATE INDEX [IX_PackageVersion_Package_PublishedAt] ON [registry].[PackageVersion] ([PackageId], [PublishedAt] DESC) INCLUDE ([SemVer], [SizeBytes], [IsDeprecated]) WHERE [IsDeleted] = 0;

CREATE UNIQUE INDEX [UQ_PackageVersion_Package_SemVer] ON [registry].[PackageVersion] ([PackageId], [SemVer]);

CREATE UNIQUE INDEX [UQ_PackageVersion_Sha256] ON [registry].[PackageVersion] ([Sha256]);

CREATE UNIQUE INDEX [UQ_Publisher_ContactEmail] ON [registry].[Publisher] ([ContactEmail]);

CREATE UNIQUE INDEX [UQ_Publisher_Slug] ON [registry].[Publisher] ([Slug]);

CREATE UNIQUE INDEX [UQ_Tag_Name] ON [registry].[Tag] ([Name]);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20261012091325_InitialCreate', N'9.0.20');

COMMIT;
GO

