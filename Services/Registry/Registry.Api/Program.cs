using System.Reflection;
using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Platform.EventBus;
using Platform.ServiceDefaults;
using Registry.Api.ErrorHandling;
using Registry.Api.Grpc;
using Registry.Api.IntegrationEvents;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Registry.Api.HealthChecks;
using Registry.Api.Metrics;
using Registry.Bll.Caching;
using Registry.Bll.Services;
using Registry.Bll.Services.Interfaces;
using Registry.Dal;
using Registry.Dal.Seeding;
using Registry.Dal.UnitOfWork;
using StackExchange.Redis;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

string connectionString = builder.Configuration.GetConnectionString("RegistryDb")
    ?? throw new InvalidOperationException("Connection string 'RegistryDb' is missing.");

Assembly businessLayer = typeof(PackageService).Assembly;

builder.Services.AddDbContext<RegistryDbContext>(options => options.UseSqlServer(connectionString));
builder.Services.AddScoped<IUnitOfWork, UnitOfWork>();

builder.Services.AddMemoryCache(options =>
{
    options.SizeLimit = 1024;
    options.TrackStatistics = true;
});
builder.AddRedisDistributedCache(
    "redis",
    settings => settings.DisableHealthChecks = true,
    options =>
    {
        options.BacklogPolicy = BacklogPolicy.FailFast;
        options.AsyncTimeout = 1000;
        options.SyncTimeout = 1000;
    }
);
builder.Services.AddSingleton<CacheMetrics>();
builder.Services.AddSingleton<TwoLevelCache>();
builder.Services.AddSingleton<RegistryMetrics>();

string redisConnectionString = builder.Configuration.GetConnectionString("redis")
    ?? throw new InvalidOperationException("Connection string 'redis' is missing.");

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: connectionString,
        name: "registry-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["database", "sql", "ready"],
        timeout: TimeSpan.FromSeconds(5)
    )
    .AddRedis(
        redisConnectionString: redisConnectionString,
        name: "redis-cache",
        failureStatus: HealthStatus.Degraded,
        tags: ["cache", "redis", "ready"],
        timeout: TimeSpan.FromSeconds(3)
    )
    .AddCheck<CacheHealthCheck>("memory-cache");
builder.Services.AddGrpcHealthChecks(options =>
    options.Services.Map(string.Empty, registration => registration.Tags.Contains("database"))
);

builder.AddRabbitMQClient("messaging", settings => settings.DisableTracing = true);
builder.Services.AddSingleton<IEventPublisher, RabbitMqEventPublisher>();
builder.Services.AddScoped<RegistryIntegrationEventService>();
builder.Services.AddScoped<IPackageEvents>(services => services.GetRequiredService<RegistryIntegrationEventService>());
builder.Services.AddHostedService<OutboxDispatcher>();

builder.Services.AddScoped<PackageService>();
builder.Services.AddScoped<IPackageService, CachedPackageService>();
builder.Services.AddScoped<TagService>();
builder.Services.AddScoped<ITagService, CachedTagService>();

builder.Services.AddAutoMapper(configuration =>
{
    configuration.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    configuration.AddMaps(businessLayer);
});

builder.Services.AddValidatorsFromAssembly(businessLayer);

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier
);
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

builder.Services.AddAuthentication(PlatformAuthentication.Scheme)
    .AddPolicyScheme(
        PlatformAuthentication.Scheme,
        "Bearer token of any trusted issuer",
        options => options.ForwardDefaultSelector = PlatformAuthentication.SchemeByIssuer(builder.Configuration)
    )
    .AddJwtBearer(PlatformAuthentication.AuthServiceScheme, options =>
    {
        options.MapInboundClaims = false;
        options.TokenValidationParameters = PlatformAuthentication.AuthServiceTokenParameters(builder.Configuration);
    });
builder.Services.AddAuthorization();

builder.Services.AddControllers();
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcLoggingInterceptor>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

await using (AsyncServiceScope scope = app.Services.CreateAsyncScope())
{
    RegistryDbContext context = scope.ServiceProvider.GetRequiredService<RegistryDbContext>();
    await context.Database.MigrateAsync();

    if (!app.Environment.IsProduction())
    {
        await DbInitializer.SeedAsync(context);
    }
}

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<RegistryGrpcService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

await app.RunAsync();
