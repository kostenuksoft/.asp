using System.Text.Json;
using System.Text.Json.Serialization;
using Cicd.Grpc;
using Microsoft.Extensions.Http.Resilience;
using Platform.Aggregator.ErrorHandling;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;
using Registry.Grpc;
using StackExchange.Redis;
using Telemetry.Grpc;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddGrpcClient<CicdService.CicdServiceClient>(options => options.Address = new Uri("https://cicd-api"));
builder.Services.AddGrpcClient<RegistryService.RegistryServiceClient>(options => options.Address = new Uri("https://registry-api"));
builder.Services.AddGrpcClient<TelemetryService.TelemetryServiceClient>(options => options.Address = new Uri("https://telemetry-api"));
builder.AddRedisDistributedCache("redis", configureOptions: options =>
{
    options.BacklogPolicy = BacklogPolicy.FailFast;
    options.AsyncTimeout = 1000;
    options.SyncTimeout = 1000;
});
builder.Services.ConfigureAll<HttpStandardResilienceOptions>(options =>
{
    options.AttemptTimeout.Timeout = TimeSpan.FromSeconds(2);
    options.Retry.MaxRetryAttempts = 1;
    options.TotalRequestTimeout.Timeout = TimeSpan.FromSeconds(5);
});
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
