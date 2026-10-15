using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Http.Resilience;
using Platform.Aggregator.Clients;
using Platform.Aggregator.ErrorHandling;
using Platform.Aggregator.Services;
using Platform.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddHttpClient<CicdClient>(client => client.BaseAddress = new("https+http://cicd-api"));
builder.Services.AddHttpClient<RegistryClient>(client => client.BaseAddress = new("https+http://registry-api"));
builder.Services.AddHttpClient<TelemetryClient>(client => client.BaseAddress = new("https+http://telemetry-api"));
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
