IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

string cicdScripts = Path.Combine(builder.AppHostDirectory, "..", "db", "p1");
string cicdCreationScript = string.Join(
    Environment.NewLine,
    File.ReadAllText(Path.Combine(cicdScripts, "schema.sql")),
    File.ReadAllText(Path.Combine(cicdScripts, "procedures.sql")),
    File.ReadAllText(Path.Combine(cicdScripts, "seed.sql"))
);

IResourceBuilder<ParameterResource> sqlPassword = builder.AddParameter("sql-password", secret: true);

IResourceBuilder<SqlServerServerResource> sql = builder.AddSqlServer("sql", sqlPassword)
    .WithDataVolume();

IResourceBuilder<SqlServerDatabaseResource> cicdDb = sql
    .AddDatabase("CicdDb", "DevHub_Cicd")
    .WithCreationScript(cicdCreationScript);

IResourceBuilder<SqlServerDatabaseResource> registryDb = sql.AddDatabase("RegistryDb", "DevHub_Registry");

IResourceBuilder<MongoDBServerResource> mongo = builder.AddMongoDB("mongo")
    .WithDataVolume();

IResourceBuilder<MongoDBDatabaseResource> telemetryDb = mongo.AddDatabase("TelemetryDb", "devhub_telemetry");

IResourceBuilder<RedisResource> redis = builder.AddRedis("redis")
    .WithDataVolume();

IResourceBuilder<RabbitMQServerResource> messaging = builder.AddRabbitMQ("messaging")
    .WithDataVolume()
    .WithManagementPlugin();

IResourceBuilder<ProjectResource> cicd = builder.AddProject<Projects.Cicd_Api>("cicd-api")
    .WithReference(cicdDb)
    .WaitFor(cicdDb)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithHttpHealthCheck("/health/ready");

IResourceBuilder<ProjectResource> registry = builder.AddProject<Projects.Registry_Api>("registry-api")
    .WithReference(registryDb)
    .WaitFor(registryDb)
    .WithReference(redis)
    .WaitFor(redis)
    .WithReference(messaging)
    .WaitFor(messaging)
    .WithHttpHealthCheck("/health/ready");

IResourceBuilder<ProjectResource> telemetry = builder.AddProject<Projects.Telemetry_WebApi>("telemetry-api")
    .WithReference(telemetryDb)
    .WaitFor(telemetryDb)
    .WithReference(messaging)
    .WaitFor(messaging)
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
    .WithHttpHealthCheck("/health/ready");

builder.AddProject<Projects.Platform_ApiGateway>("gateway")
    .WithHttpEndpoint(port: 5000, name: "public")
    .WithReference(cicd)
    .WithReference(registry)
    .WithReference(telemetry)
    .WithReference(aggregator)
    .WaitFor(aggregator)
    .WithHttpHealthCheck("/health/ready");

builder
    .Build()
    .Run();
