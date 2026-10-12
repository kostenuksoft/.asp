IDistributedApplicationBuilder builder = DistributedApplication.CreateBuilder(args);

string cicdScripts = Path.Combine(builder.AppHostDirectory, "..", "db", "p1");
string cicdCreationScript = string.Join(
    Environment.NewLine,
    File.ReadAllText(Path.Combine(cicdScripts, "schema.sql")),
    File.ReadAllText(Path.Combine(cicdScripts, "procedures.sql")),
    File.ReadAllText(Path.Combine(cicdScripts, "seed.sql")));

IResourceBuilder<SqlServerServerResource> sql = builder.AddSqlServer("sql");

IResourceBuilder<SqlServerDatabaseResource> cicdDb = sql
    .AddDatabase("CicdDb", "DevHub_Cicd")
    .WithCreationScript(cicdCreationScript);

builder.AddProject<Projects.Cicd_Api>("cicd-api")
    .WithReference(cicdDb)
    .WaitFor(cicdDb);

builder.Build().Run();
