USE DevHub_Cicd;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

IF SUSER_ID(N'devhub_cicd_app') IS NULL
    CREATE LOGIN devhub_cicd_app
        WITH PASSWORD = N'$(CicdAppPassword)',
             DEFAULT_DATABASE = DevHub_Cicd,
             CHECK_POLICY = ON,
             CHECK_EXPIRATION = OFF;

IF DATABASE_PRINCIPAL_ID(N'cicd_app_role') IS NULL
    CREATE ROLE cicd_app_role;

IF DATABASE_PRINCIPAL_ID(N'cicd_reader_role') IS NULL
    CREATE ROLE cicd_reader_role;

IF DATABASE_PRINCIPAL_ID(N'cicd_app') IS NULL
    CREATE USER cicd_app FOR LOGIN devhub_cicd_app WITH DEFAULT_SCHEMA = cicd;

IF DATABASE_PRINCIPAL_ID(N'cicd_reader') IS NULL
    CREATE USER cicd_reader WITHOUT LOGIN WITH DEFAULT_SCHEMA = cicd;

IF IS_ROLEMEMBER(N'cicd_app_role', N'cicd_app') = 0
    ALTER ROLE cicd_app_role ADD MEMBER cicd_app;

IF IS_ROLEMEMBER(N'cicd_reader_role', N'cicd_reader') = 0
    ALTER ROLE cicd_reader_role ADD MEMBER cicd_reader;
GO

GRANT SELECT, INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::cicd TO cicd_app_role;
GRANT EXECUTE ON TYPE::cicd.ArtifactReference TO cicd_app_role;
GRANT SELECT ON SCHEMA::cicd TO cicd_reader_role;
DENY INSERT, UPDATE, DELETE, EXECUTE ON SCHEMA::cicd TO cicd_reader_role;
GO

SELECT
    p.name AS Principal,
    dp.permission_name AS Permission,
    dp.state_desc AS State,
    ISNULL(SCHEMA_NAME(dp.major_id), TYPE_NAME(dp.major_id)) AS Securable
FROM sys.database_permissions AS dp
INNER JOIN sys.database_principals AS p ON p.principal_id = dp.grantee_principal_id
WHERE p.name IN (N'cicd_app_role', N'cicd_reader_role')
ORDER BY p.name, dp.state_desc DESC, dp.permission_name;
GO
