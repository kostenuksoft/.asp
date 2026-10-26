using Microsoft.AspNetCore.Authorization;
using Platform.ServiceDefaults;
using Secrets.Api.Authorization;
using Secrets.Api.Store;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

builder.Services.AddSingleton(TimeProvider.System);
builder.Services.AddSingleton<PipelineSecretStore>();

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
        options.Audience = "secrets_api";
        options.MapInboundClaims = false;
    })
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: "DevHub",
        authenticationScheme: PlatformAuthentication.KeycloakScheme,
        options =>
        {
            options.Audience = "secrets-api";
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
});
builder.Services.AddSingleton<IAuthorizationHandler, KeycloakPermissionHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, ScopePermissionHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, RolePermissionHandler>();

builder.Services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
    context.ProblemDetails.Extensions["traceId"] = context.HttpContext.TraceIdentifier
);
builder.Services.AddControllers();

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();
app.UseExceptionHandler();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapDefaultEndpoints();

await app.RunAsync();
