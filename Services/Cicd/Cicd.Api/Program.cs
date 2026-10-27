using System.Reflection;
using Cicd.Api.Authorization;
using Cicd.Api.ErrorHandling;
using Cicd.Api.Grpc;
using Cicd.Api.HealthChecks;
using Cicd.Api.IntegrationEvents;
using Cicd.Api.IntegrationEvents.Events;
using Cicd.Api.IntegrationEvents.Handlers;
using Cicd.Api.Metrics;
using Cicd.Bll.Services;
using Cicd.Bll.Services.Interfaces;
using Cicd.Dal.UnitOfWork;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.OpenApi;
using Platform.EventBus;
using Platform.ServiceDefaults;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

string connectionString = builder.Configuration.GetConnectionString("CicdDb")
    ?? throw new InvalidOperationException("Connection string 'CicdDb' is missing.");

Assembly businessLayer = typeof(BuildRunService).Assembly;

builder.Services.AddScoped<IUnitOfWork>(_ => new UnitOfWork(connectionString));
builder.Services.AddScoped<IPipelineService, PipelineService>();
builder.Services.AddScoped<IBuildRunService, BuildRunService>();
builder.Services.AddScoped<IAgentService, AgentService>();
builder.Services.AddSingleton<CicdMetrics>();

builder.AddRabbitMQClient("messaging", settings => settings.DisableTracing = true);
builder.Services.AddScoped<IIntegrationEventHandler<PackageCreatedEvent>, PackageCreatedHandler>();
builder.Services.AddScoped<IIntegrationEventHandler<PackageRenamedEvent>, PackageRenamedHandler>();
builder.Services.AddScoped<
    IIntegrationEventHandler<PackageLicensePriceChangedEvent>,
    PackageLicensePriceChangedHandler
>();
builder.Services.AddScoped<IIntegrationEventHandler<PackageDeletedEvent>, PackageDeletedHandler>();
builder.Services.AddHostedService<RegistryEventsConsumer>();

builder.Services.AddHealthChecks()
    .AddSqlServer(
        connectionString: connectionString,
        name: "cicd-db",
        failureStatus: HealthStatus.Unhealthy,
        tags: ["database", "sql", "ready"],
        timeout: TimeSpan.FromSeconds(5)
    )
    .AddCheck<AgentPoolHealthCheck>(
        "agent-pool",
        failureStatus: HealthStatus.Degraded,
        tags: ["ready"],
        timeout: TimeSpan.FromSeconds(5)
    );
builder.Services.AddGrpcHealthChecks(options =>
    options.Services.Map(string.Empty, registration => registration.Tags.Contains("database"))
);

builder.Services.AddAutoMapper(configuration =>
{
    configuration.LicenseKey = builder.Configuration["AutoMapper:LicenseKey"];
    configuration.AddMaps(businessLayer);
});

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier
);
builder.Services.AddExceptionHandler<DomainExceptionHandler>();

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
        options.Audience = "cicd_api";
        options.MapInboundClaims = false;
    })
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: "DevHub",
        authenticationScheme: PlatformAuthentication.KeycloakScheme,
        options =>
        {
            options.Audience = "cicd-api";
            options.MapInboundClaims = false;
            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
        }
    );
builder.Services.AddAuthorization(options =>
{
    foreach (string permission in Permissions.All)
    {
        options.AddPolicy(permission, policy => policy.Requirements.Add(new PermissionRequirement(permission)));
    }

    options.AddPolicy(Permissions.ReadScope, policy => policy.RequireClaim("scope", Permissions.ReadScope));
});
builder.Services.AddSingleton<IAuthorizationHandler, KeycloakPermissionHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ScopePermissionHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, RolePermissionHandler>();

builder.Services.AddControllers();
builder.Services.AddGrpc(options => options.Interceptors.Add<GrpcLoggingInterceptor>());
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    string authority = builder.Configuration["IdentityServer:Authority"] ?? "https://localhost:5005";
    options.AddSecurityDefinition(PlatformAuthentication.IdentityServerScheme, new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            AuthorizationCode = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{authority}/connect/authorize"),
                TokenUrl = new Uri($"{authority}/connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    [Permissions.ReadScope] = "Read pipelines, build runs and agents",
                    [Permissions.WriteScope] = "Queue, start, change and delete build runs and pipelines",
                },
            },
        },
    });
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        [new OpenApiSecuritySchemeReference(PlatformAuthentication.IdentityServerScheme, document)] =
            [Permissions.ReadScope, Permissions.WriteScope],
    });
});

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.OAuthClientId("swagger");
        options.OAuthUsePkce();
        options.OAuthScopes(Permissions.ReadScope, Permissions.WriteScope);
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapGrpcService<CicdGrpcService>();
app.MapGrpcHealthChecksService();
app.MapDefaultEndpoints();

app.Run();
