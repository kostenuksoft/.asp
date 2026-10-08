USE DevHub_Registry;
GO

SET ANSI_NULLS ON;
SET QUOTED_IDENTIFIER ON;
SET NOCOUNT ON;
GO

IF SUSER_ID(N'devhub_registry_app') IS NULL
    CREATE LOGIN devhub_registry_app
        WITH PASSWORD = N'$(RegistryAppPassword)',
             DEFAULT_DATABASE = DevHub_Registry,
             CHECK_POLICY = ON,
             CHECK_EXPIRATION = OFF;

IF DATABASE_PRINCIPAL_ID(N'registry_app_role') IS NULL
    CREATE ROLE registry_app_role;

IF DATABASE_PRINCIPAL_ID(N'registry_reader_role') IS NULL
    CREATE ROLE registry_reader_role;

IF DATABASE_PRINCIPAL_ID(N'registry_app') IS NULL
    CREATE USER registry_app FOR LOGIN devhub_registry_app WITH DEFAULT_SCHEMA = registry;

IF DATABASE_PRINCIPAL_ID(N'registry_reader') IS NULL
    CREATE USER registry_reader WITHOUT LOGIN WITH DEFAULT_SCHEMA = registry;

IF IS_ROLEMEMBER(N'registry_app_role', N'registry_app') = 0
    ALTER ROLE registry_app_role ADD MEMBER registry_app;

IF IS_ROLEMEMBER(N'registry_reader_role', N'registry_reader') = 0
    ALTER ROLE registry_reader_role ADD MEMBER registry_reader;
GO

GRANT SELECT, INSERT, UPDATE, DELETE ON SCHEMA::registry TO registry_app_role;
GRANT SELECT ON SCHEMA::registry TO registry_reader_role;
DENY INSERT, UPDATE, DELETE ON SCHEMA::registry TO registry_reader_role;
GO

SELECT
    p.name AS Principal,
    dp.permission_name AS Permission,
    dp.state_desc AS State,
    SCHEMA_NAME(dp.major_id) AS Securable
FROM sys.database_permissions AS dp
INNER JOIN sys.database_principals AS p ON p.principal_id = dp.grantee_principal_id
WHERE p.name IN (N'registry_app_role', N'registry_reader_role')
ORDER BY p.name, dp.state_desc DESC, dp.permission_name;
GO
