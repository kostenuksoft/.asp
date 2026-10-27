using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.IdentityServer.Data;
using Platform.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

string connectionString = builder.Configuration.GetConnectionString("IdentityServerDb")
    ?? throw new InvalidOperationException("Connection string 'IdentityServerDb' is missing.");
string issuerUri = builder.Configuration["IdentityServer:IssuerUri"]
    ?? throw new InvalidOperationException("Configuration value 'IdentityServer:IssuerUri' is missing.");
string migrationsAssembly = typeof(ApplicationDbContext).Assembly.GetName().Name!;

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(connectionString, sql => sql.MigrationsAssembly(migrationsAssembly))
);

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddDefaultTokenProviders();

builder.Services.AddIdentityServer(options =>
    {
        options.IssuerUri = issuerUri;
        options.Events.RaiseErrorEvents = true;
        options.Events.RaiseFailureEvents = true;
        options.Events.RaiseSuccessEvents = true;
    })
    .AddAspNetIdentity<ApplicationUser>()
    .AddConfigurationStore(options =>
        options.ConfigureDbContext = context => context.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(migrationsAssembly)
        )
    )
    .AddOperationalStore(options =>
    {
        options.ConfigureDbContext = context => context.UseSqlServer(
            connectionString,
            sql => sql.MigrationsAssembly(migrationsAssembly)
        );
        options.EnableTokenCleanup = true;
    });

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: connectionString,
        name: "identityserver-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["database", "sql", "ready"],
        timeout: TimeSpan.FromSeconds(5)
    );

builder.Services.AddRazorPages();

WebApplication app = builder.Build();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    await IdentityServerSeeder.SeedAsync(scope.ServiceProvider, app.Configuration);
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseStaticFiles();
app.UseRouting();
app.UseIdentityServer();
app.UseAuthorization();

app.MapRazorPages().RequireAuthorization();
app.MapDefaultEndpoints();

await app.RunAsync();
