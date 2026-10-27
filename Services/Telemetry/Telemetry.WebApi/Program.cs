using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Platform.EventBus;
using Platform.ServiceDefaults;
using Telemetry.Application;
using Telemetry.Application.Common.Mappings;
using Telemetry.Infrastructure;
using Telemetry.Infrastructure.Persistence;
using Telemetry.WebApi.ErrorHandling;
using Telemetry.WebApi.Grpc;
using Telemetry.WebApi.IntegrationEvents;
using Telemetry.WebApi.IntegrationEvents.Events;
using Telemetry.WebApi.IntegrationEvents.Handlers;
using Telemetry.WebApi.Metrics;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

string? telemetryDb = builder.Configuration.GetConnectionString("TelemetryDb");

builder.Services
    .AddApplication(builder.Configuration)
    .AddInfrastructure(builder.Configuration);

if (!string.IsNullOrEmpty(telemetryDb))
{
    builder.Services.PostConfigure<MongoDbOptions>(options => options.ConnectionString = telemetryDb);
}

builder.Services.AddAutoMapper(options =>
{
    options.LicenseKey = builder.Configuration["LuckyPenny:LicenseKey"];
    options.AddMaps(typeof(MappingProfile).Assembly);
});

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
    })
    .AddJwtBearer(PlatformAuthentication.IdentityServerScheme, options =>
    {
        options.Authority = builder.Configuration["IdentityServer:Authority"];
        options.Audience = "telemetry_api";
        options.MapInboundClaims = false;
    })
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: "DevHub",
        authenticationScheme: PlatformAuthentication.KeycloakScheme,
        options =>
        {
            options.Audience = "telemetry-api";
            options.MapInboundClaims = false;
            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
        }
    );

builder.Services.AddAuthorization(options =>
    options.AddPolicy(
        TelemetryGrpcService.ReadScope,
        policy => policy.RequireClaim("scope", TelemetryGrpcService.ReadScope)
    )
);

builder.Services.AddControllers().AddJsonOptions(options =>
    options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase))
);
builder.Services.AddSingleton<TelemetryMetrics>();

builder.AddRabbitMQClient("messaging", settings => settings.DisableTracing = true);
builder.Services.AddScoped<IIntegrationEventHandler<PackageRenamedEvent>, PackageRenamedHandler>();
builder.Services.AddHostedService<RegistryEventsConsumer>();

builder.Services.AddHealthChecks()
    .AddMongoDb(
        provider => provider.GetRequiredService<MongoDbContext>().Database,
        name: "telemetry-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["database", "mongodb", "ready"],
        timeout: TimeSpan.FromSeconds(5)
    );

builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcLoggingInterceptor>());
builder.Services.AddGrpcHealthChecks(options =>
    options.Services.Map(string.Empty, registration => registration.Tags.Contains("database"))
);
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

WebApplication app = builder.Build();

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
app.MapGrpcService<TelemetryGrpcService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

await app.RunAsync();
