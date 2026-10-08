USE DevHub_Registry;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
SET XACT_ABORT OFF;
GO

IF OBJECT_ID(N'tempdb..#Check') IS NOT NULL DROP TABLE #Check;
CREATE TABLE #Check (CheckId INT IDENTITY(1, 1), Name NVARCHAR(200), Passed BIT, Detail NVARCHAR(400));
GO

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'no foreign key leaves this database',
       CASE WHEN COUNT(*) = 0 THEN 1 ELSE 0 END,
       CONCAT(COUNT(*), N' foreign key(s) reference objects outside DevHub_Registry')
FROM sys.foreign_keys AS fk
WHERE NOT EXISTS (SELECT 1 FROM sys.objects AS o WHERE o.object_id = fk.referenced_object_id);

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'4 to 6 entities',
       CASE WHEN COUNT(*) BETWEEN 4 AND 6 THEN 1 ELSE 0 END,
       CONCAT(COUNT(*), N' tables in schema registry')
FROM sys.tables
WHERE schema_id = SCHEMA_ID(N'registry');

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'1:1 PackageManifest: FK is also the PK',
       CASE WHEN COUNT(*) = 1 THEN 1 ELSE 0 END,
       N'PK_PackageManifest and FK_PackageManifest_PackageVersion share PackageVersionId'
FROM sys.index_columns AS ic
INNER JOIN sys.indexes AS i ON i.object_id = ic.object_id AND i.index_id = ic.index_id AND i.is_primary_key = 1
INNER JOIN sys.foreign_key_columns AS fkc ON fkc.parent_object_id = ic.object_id AND fkc.parent_column_id = ic.column_id
WHERE ic.object_id = OBJECT_ID(N'registry.PackageManifest');

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'M:N PackageTag: two FKs, composite key and its own field',
       CASE
           WHEN (SELECT COUNT(*) FROM sys.foreign_keys WHERE parent_object_id = OBJECT_ID(N'registry.PackageTag')) = 2
            AND COL_LENGTH(N'registry.PackageTag', N'AssignedAt') IS NOT NULL
            AND (SELECT COUNT(*) FROM sys.index_columns AS ic
                 INNER JOIN sys.indexes AS i ON i.object_id = ic.object_id AND i.index_id = ic.index_id AND i.is_primary_key = 1
                 WHERE ic.object_id = OBJECT_ID(N'registry.PackageTag')) = 2
           THEN 1 ELSE 0
       END,
       N'PK (PackageId, TagId), own field AssignedAt';

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'every index states the query it serves',
       CASE WHEN SUM(CASE WHEN ep.value IS NULL THEN 1 ELSE 0 END) = 0 THEN 1 ELSE 0 END,
       CONCAT(COUNT(*), N' IX_ index(es), ', SUM(CASE WHEN ep.value IS NULL THEN 1 ELSE 0 END), N' without MS_Description')
FROM sys.indexes AS i
INNER JOIN sys.tables AS t ON t.object_id = i.object_id AND t.schema_id = SCHEMA_ID(N'registry')
LEFT JOIN sys.extended_properties AS ep
    ON ep.major_id = i.object_id AND ep.minor_id = i.index_id AND ep.class = 7 AND ep.name = N'MS_Description'
WHERE i.name LIKE N'IX[_]%';

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'reference data seeded: publishers and tags',
       CASE WHEN (SELECT COUNT(*) FROM registry.Publisher) >= 3 AND (SELECT COUNT(*) FROM registry.Tag) >= 5 THEN 1 ELSE 0 END,
       CONCAT((SELECT COUNT(*) FROM registry.Publisher), N' publishers, ', (SELECT COUNT(*) FROM registry.Tag), N' tags');
GO

DECLARE @Error INT = 0;
BEGIN TRY
    INSERT INTO registry.Publisher (Slug, DisplayName, ContactEmail) VALUES (N'platform-team', N'Duplicate', N'dup@devhub.local');
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
END CATCH;
INSERT INTO #Check (Name, Passed, Detail)
VALUES (N'UNIQUE rejects a duplicate publisher slug', CASE WHEN @Error = 2627 THEN 1 ELSE 0 END, CONCAT(N'error ', @Error));

SET @Error = 0;
BEGIN TRY
    INSERT INTO registry.PackageVersion (PackageId, SemVer, MajorVersion, MinorVersion, PatchVersion, SizeBytes, Sha256)
    SELECT PackageId, N'3.0.0', 2, 9, 9, 10, 'abababababababababababababababababababababababababababababababab'
    FROM registry.Package WHERE Name = N'devhub-auth';
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
END CATCH;
INSERT INTO #Check (Name, Passed, Detail)
VALUES (N'CHECK rejects a SemVer that disagrees with its parts', CASE WHEN @Error = 547 THEN 1 ELSE 0 END, CONCAT(N'error ', @Error));

SET @Error = 0;
BEGIN TRY
    INSERT INTO registry.PackageVersion (PackageId, SemVer, MajorVersion, MinorVersion, PatchVersion, SizeBytes, Sha256)
    SELECT PackageId, N'3.0.0', 3, 0, 0, 0, 'cdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcdcd'
    FROM registry.Package WHERE Name = N'devhub-auth';
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
END CATCH;
INSERT INTO #Check (Name, Passed, Detail)
VALUES (N'CHECK rejects a non-positive package size', CASE WHEN @Error = 547 THEN 1 ELSE 0 END, CONCAT(N'error ', @Error));

SET @Error = 0;
BEGIN TRY
    INSERT INTO registry.PackageManifest (PackageVersionId, LicenseExpression, TargetFramework, EntryPoints)
    SELECT TOP (1) PackageVersionId, N'MIT', N'net9.0', N'{"lib":' FROM registry.PackageVersion
    WHERE NOT EXISTS (SELECT 1 FROM registry.PackageManifest AS m WHERE m.PackageVersionId = registry.PackageVersion.PackageVersionId);
    IF @@ROWCOUNT = 0
        INSERT INTO registry.PackageManifest (PackageVersionId, LicenseExpression, TargetFramework, EntryPoints)
        VALUES (-1, N'MIT', N'net9.0', N'{"lib":');
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
END CATCH;
INSERT INTO #Check (Name, Passed, Detail)
VALUES (N'manifest rejects invalid JSON or an orphan version', CASE WHEN @Error = 547 THEN 1 ELSE 0 END, CONCAT(N'error ', @Error));
GO

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'latest-version view skips deprecated and older versions',
       CASE WHEN SemVer = N'2.1.0' THEN 1 ELSE 0 END,
       CONCAT(N'devhub-auth latest = ', SemVer)
FROM registry.vw_LatestPackageVersion
WHERE PackageKey = N'platform-team/devhub-auth';

INSERT INTO #Check (Name, Passed, Detail)
SELECT N'M:N query through the junction and its own field',
       CASE WHEN COUNT(*) = 3 AND MIN(pt.AssignedAt) IS NOT NULL THEN 1 ELSE 0 END,
       CONCAT(COUNT(*), N' package(s) tagged infrastructure')
FROM registry.Tag AS t
INNER JOIN registry.PackageTag AS pt ON pt.TagId = t.TagId
INNER JOIN registry.Package AS p ON p.PackageId = pt.PackageId
WHERE t.Name = N'infrastructure'
  AND p.IsDeleted = 0;
GO

DECLARE @Cascaded BIT;
DECLARE @PackageId BIGINT = (SELECT PackageId FROM registry.Package WHERE Name = N'graph-explorer');

BEGIN TRANSACTION;

DELETE FROM registry.Package WHERE PackageId = @PackageId;

SET @Cascaded =
    CASE
        WHEN NOT EXISTS (SELECT 1 FROM registry.PackageVersion WHERE PackageId = @PackageId)
         AND NOT EXISTS (SELECT 1 FROM registry.PackageTag WHERE PackageId = @PackageId)
         AND EXISTS (SELECT 1 FROM registry.Tag WHERE Name = N'experimental')
         AND EXISTS (SELECT 1 FROM registry.Publisher WHERE Slug = N'research-team')
        THEN 1 ELSE 0
    END;

ROLLBACK TRANSACTION;

INSERT INTO #Check (Name, Passed, Detail)
VALUES (N'deleting a package cascades to versions, manifests and tag links only', @Cascaded, N'tag and publisher survive; rolled back');
GO

DECLARE @Error INT = 0;
BEGIN TRANSACTION;
BEGIN TRY
    DELETE FROM registry.Publisher WHERE Slug = N'platform-team';
END TRY
BEGIN CATCH
    SET @Error = ERROR_NUMBER();
END CATCH;
IF @@TRANCOUNT > 0 ROLLBACK TRANSACTION;

INSERT INTO #Check (Name, Passed, Detail)
VALUES (N'a publisher with packages cannot be removed', CASE WHEN @Error = 547 THEN 1 ELSE 0 END, CONCAT(N'error ', @Error));
GO

IF DATABASE_PRINCIPAL_ID(N'registry_reader') IS NOT NULL AND DATABASE_PRINCIPAL_ID(N'registry_app') IS NOT NULL AND SUSER_ID(N'devhub_registry_app') IS NOT NULL
BEGIN
    DECLARE @ReaderError INT = 0, @AppRows INT = -1, @CrossError INT = 0;
    DECLARE @OtherDatabase SYSNAME = CASE WHEN DB_ID(N'DevHub_Cicd') IS NOT NULL THEN N'DevHub_Cicd' ELSE N'model' END;
    DECLARE @CrossProbe NVARCHAR(400) = CONCAT(N'SELECT COUNT(*) FROM ', QUOTENAME(@OtherDatabase), N'.sys.objects;');
    DECLARE @LoginMapped BIT =
        CASE WHEN EXISTS
        (
            SELECT 1
            FROM sys.database_principals AS u
            INNER JOIN sys.server_principals AS l ON l.sid = u.sid
            WHERE u.name = N'registry_app' AND l.name = N'devhub_registry_app' AND l.default_database_name = N'DevHub_Registry'
        ) THEN 1 ELSE 0 END;

    EXECUTE AS USER = N'registry_reader';
    BEGIN TRY
        DELETE FROM registry.Tag WHERE 1 = 0;
    END TRY
    BEGIN CATCH
        SET @ReaderError = ERROR_NUMBER();
    END CATCH;
    REVERT;

    EXECUTE AS LOGIN = N'devhub_registry_app';
    SET @AppRows = (SELECT COUNT(*) FROM registry.Package);
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
        (N'least privilege: a separate app login is mapped to this database', @LoginMapped, N'devhub_registry_app -> registry_app, default database DevHub_Registry'),
        (N'least privilege: the app login reaches its own database only', CASE WHEN @AppRows >= 0 AND @CrossError = 916 THEN 1 ELSE 0 END, CONCAT(N'error ', @CrossError, N' reading ', @OtherDatabase));
END
ELSE
    INSERT INTO #Check (Name, Passed, Detail)
    VALUES (N'least privilege: login and roles exist (run security.sql before verify.sql)', 0, N'devhub_registry_app / registry_app / registry_reader missing');
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
