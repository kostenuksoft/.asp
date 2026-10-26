using System.Text.Json;
using System.Text.Json.Serialization;
using Cicd.Grpc;
using Grpc.Health.V1;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.Aggregator.ErrorHandling;
using Platform.Aggregator.HealthChecks;
using Platform.Aggregator.Resilience;
using Platform.Aggregator.Security;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;
using Registry.Grpc;
using StackExchange.Redis;
using Telemetry.Grpc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.Configure<ClientCredentialsOptions>(builder.Configuration.GetSection(ClientCredentialsOptions.SectionName));
builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddHttpClient(ClientCredentialsTokenProvider.HttpClientName);
builder.Services.AddSingleton<ClientCredentialsTokenProvider>();
builder.Services.AddTransient<ClientCredentialsHandler>();

builder.Services.AddGrpcClient<CicdService.CicdServiceClient>(options => options.Address = new Uri("https://cicd-api"))
    .AddHttpMessageHandler<ClientCredentialsHandler>()
    .WithGrpcRetry(ServicePolicy.Critical)
    .WithServicePolicy("cicd-api", ServicePolicy.Critical);
builder.Services.AddGrpcClient<RegistryService.RegistryServiceClient>(options => options.Address = new Uri("https://registry-api"))
    .AddHttpMessageHandler<ClientCredentialsHandler>()
    .WithGrpcRetry(ServicePolicy.Critical)
    .WithServicePolicy("registry-api", ServicePolicy.Critical);
builder.Services.AddGrpcClient<TelemetryService.TelemetryServiceClient>(options => options.Address = new Uri("https://telemetry-api"))
    .AddHttpMessageHandler<ClientCredentialsHandler>()
    .WithGrpcRetry(ServicePolicy.NonCritical)
    .WithServicePolicy("telemetry-api", ServicePolicy.NonCritical);

builder.Services.AddGrpcClient<Health.HealthClient>("cicd-api", options => options.Address = new Uri("https://cicd-api"));
builder.Services.AddGrpcClient<Health.HealthClient>("registry-api", options => options.Address = new Uri("https://registry-api"));
builder.Services.AddGrpcClient<Health.HealthClient>("telemetry-api", options => options.Address = new Uri("https://telemetry-api"));

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

string redisConnectionString = builder.Configuration.GetConnectionString("redis")
    ?? throw new InvalidOperationException("Connection string 'redis' is missing.");

builder.Services.AddHealthChecks()
    .AddTypeActivatedCheck<GrpcDependencyHealthCheck>(
        "cicd-api",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["grpc", "downstream", "ready"],
        args: "cicd-api"
    )
    .AddTypeActivatedCheck<GrpcDependencyHealthCheck>(
        "registry-api",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["grpc", "downstream", "ready"],
        args: "registry-api"
    )
    .AddTypeActivatedCheck<GrpcDependencyHealthCheck>(
        "telemetry-api",
        failureStatus: HealthStatus.Degraded,
        tags: ["grpc", "downstream", "ready"],
        args: "telemetry-api"
    )
    .AddRedis(
        redisConnectionString: redisConnectionString,
        name: "redis-cache",
        failureStatus: HealthStatus.Degraded,
        tags: ["cache", "redis", "ready"],
        timeout: TimeSpan.FromSeconds(3)
    );

builder.Services.AddScoped<BuildRunCardService>();

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier
);
builder.Services.AddExceptionHandler<UpstreamExceptionHandler>();

builder.Services.AddControllers().AddJsonOptions(options =>
{
    options.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
});

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();
