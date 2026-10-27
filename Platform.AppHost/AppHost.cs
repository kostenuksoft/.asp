IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

string cicdScripts = Path.Combine(builder.AppHostDirectory, "..", "db", "p1");
string cicdCreationScript = string.Join(
    Environment.NewLine,
    File.ReadAllText(Path.Combine(cicdScripts, "schema.sql")),
    File.ReadAllText(Path.Combine(cicdScripts, "procedures.sql")),
    File.ReadAllText(Path.Combine(cicdScripts, "seed.sql"))
);

const string ContainerGroupLabel = "com.docker.compose.project=devhub";

IResourceBuilder<ParameterResource> sqlPassword = builder.AddParameter("sql-password", secret: true);

IResourceBuilder<SqlServerServerResource> sql = builder.AddSqlServer("sql", sqlPassword)
    .WithDataVolume()
    .WithContainerName("devhub-sql")
    .WithContainerRuntimeArgs("--label", ContainerGroupLabel)
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<SqlServerDatabaseResource> cicdDb = sql
    .AddDatabase("CicdDb", "DevHub_Cicd")
    .WithCreationScript(cicdCreationScript);

IResourceBuilder<SqlServerDatabaseResource> registryDb = sql.AddDatabase("RegistryDb", "DevHub_Registry");

IResourceBuilder<MongoDBServerResource> mongo = builder.AddMongoDB("mongo")
    .WithDataVolume()
    .WithContainerName("devhub-mongo")
    .WithContainerRuntimeArgs("--label", ContainerGroupLabel)
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<MongoDBDatabaseResource> telemetryDb = mongo.AddDatabase("TelemetryDb", "devhub_telemetry");

IResourceBuilder<RedisResource> redis = builder.AddRedis("redis")
    .WithDataVolume()
    .WithContainerName("devhub-redis")
    .WithContainerRuntimeArgs("--label", ContainerGroupLabel)
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<RabbitMQServerResource> messaging = builder.AddRabbitMQ("messaging")
    .WithDataVolume()
    .WithManagementPlugin()
    .WithContainerName("devhub-rabbitmq")
    .WithContainerRuntimeArgs("--label", ContainerGroupLabel)
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<ParameterResource> identitySqlPassword = builder.AddParameter("identity-sql-password", secret: true);

IResourceBuilder<SqlServerServerResource> identitySql = builder.AddSqlServer("identity-sql", identitySqlPassword)
    .WithDataVolume()
    .WithContainerName("devhub-identity-sql")
    .WithContainerRuntimeArgs("--label", ContainerGroupLabel)
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<SqlServerDatabaseResource> identityDb = identitySql.AddDatabase("IdentityDb", "DevHub_Identity");

IResourceBuilder<ParameterResource> jwtSecret = builder.AddParameter("jwt-secret", secret: true);
IResourceBuilder<ParameterResource> authAdminPassword = builder.AddParameter("auth-admin-password", secret: true);

IResourceBuilder<ProjectResource> auth = builder.AddProject<Projects.Auth_Api>("auth-api")
    .WithReference(identityDb)
    .WaitFor(identityDb)
    .WithEnvironment("Jwt__SecretKey", jwtSecret)
    .WithEnvironment("Auth__AdminPassword", authAdminPassword)
    .WithHttpHealthCheck("/health/ready");

IResourceBuilder<SqlServerDatabaseResource> identityServerDb = identitySql.AddDatabase(
    "IdentityServerDb",
    "DevHub_IdentityServer"
);

IResourceBuilder<ParameterResource> postmanClientSecret = builder.AddParameter("postman-client-secret", secret: true);
IResourceBuilder<ParameterResource> aggregatorClientSecret = builder.AddParameter(
    "aggregator-client-secret",
    secret: true
);
IResourceBuilder<ParameterResource> identityServerUserPassword = builder.AddParameter(
    "identityserver-user-password",
    secret: true
);

IResourceBuilder<ProjectResource> identityServer = builder
    .AddProject<Projects.Platform_IdentityServer>("identityserver")
    .WithReference(identityServerDb)
    .WaitFor(identityServerDb)
    .WithEnvironment("IdentityServer__Clients__PostmanSecret", postmanClientSecret)
    .WithEnvironment("IdentityServer__Clients__AggregatorSecret", aggregatorClientSecret)
    .WithEnvironment("IdentityServer__SeedUser__Password", identityServerUserPassword)
    .WithHttpHealthCheck("/health/ready");

EndpointReference identityServerEndpoint = identityServer.GetEndpoint("https");

IResourceBuilder<ParameterResource> keycloakAdminPassword = builder.AddParameter(
    "keycloak-admin-password",
    secret: true
);

IResourceBuilder<KeycloakResource> keycloak = builder
    .AddKeycloak("keycloak", 8080, adminPassword: keycloakAdminPassword)
    .WithDataVolume()
    .WithContainerName("devhub-keycloak")
    .WithContainerRuntimeArgs("--label", ContainerGroupLabel)
    .WithLifetime(ContainerLifetime.Persistent);

IResourceBuilder<ProjectResource> cicd = builder.AddProject<Projects.Cicd_Api>("cicd-api")
    .WithReference(cicdDb)
    .WaitFor(cicdDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__SecretKey", jwtSecret)
    .WithEnvironment("IdentityServer__Authority", identityServerEndpoint)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health/ready");

IResourceBuilder<ProjectResource> registry = builder.AddProject<Projects.Registry_Api>("registry-api")
    .WithReference(registryDb)
    .WaitFor(registryDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__SecretKey", jwtSecret)
    .WithEnvironment("IdentityServer__Authority", identityServerEndpoint)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health/ready");

IResourceBuilder<ProjectResource> telemetry = builder.AddProject<Projects.Telemetry_WebApi>("telemetry-api")
    .WithReference(telemetryDb)
    .WaitFor(telemetryDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithEnvironment("Jwt__SecretKey", jwtSecret)
    .WithEnvironment("IdentityServer__Authority", identityServerEndpoint)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health/ready");

IResourceBuilder<ProjectResource> secrets = builder.AddProject<Projects.Secrets_Api>("secrets-api")
    .WithEnvironment("Jwt__SecretKey", jwtSecret)
    .WithEnvironment("IdentityServer__Authority", identityServerEndpoint)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health/ready");

IResourceBuilder<ProjectResource> aggregator = builder.AddProject<Projects.Platform_Aggregator>("aggregator")
    .WithReference(cicd)
    .WithReference(registry)
    .WithReference(telemetry)
    .WithReference(redis)
    .WaitFor(cicd)
    .WaitFor(registry)
    .WaitFor(telemetry)
    .WaitFor(redis)
    .WaitFor(identityServer)
    .WithEnvironment("IdentityServer__Authority", identityServerEndpoint)
    .WithEnvironment("IdentityServer__ClientSecret", aggregatorClientSecret)
    .WithHttpHealthCheck("/health/ready");

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(auth)
    .WithReference(cicd)
    .WithReference(registry)
    .WithReference(telemetry)
    .WithReference(secrets)
    .WithReference(aggregator)
    .WaitFor(auth)
    .WaitFor(aggregator)
    .WithEnvironment("Jwt__SecretKey", jwtSecret)
    .WithEnvironment("IdentityServer__Authority", identityServerEndpoint)
    .WithReference(keycloak)
    .WaitFor(keycloak)
    .WithHttpHealthCheck("/health/ready");

builder
    .Build()
    .Run();
