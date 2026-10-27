using System.Diagnostics;
using Platform.ServiceDefaults;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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
        options.MapInboundClaims = false;
        options.TokenValidationParameters.ValidAudiences = ["cicd_api", "registry_api", "telemetry_api", "secrets_api"];
    })
    .AddKeycloakJwtBearer(
        serviceName: "keycloak",
        realm: "DevHub",
        authenticationScheme: PlatformAuthentication.KeycloakScheme,
        options =>
        {
            options.MapInboundClaims = false;
            options.TokenValidationParameters.ValidAudiences =
                ["cicd-api", "registry-api", "telemetry-api", "secrets-api"];
            if (builder.Environment.IsDevelopment())
            {
                options.RequireHttpsMetadata = false;
            }
        }
    );
builder.Services.AddAuthorization();

builder.Services.AddReverseProxy()
    .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"))
    .AddServiceDiscoveryDestinationResolver();

WebApplication app = builder.Build();

app.UseMiddleware<CorrelationIdMiddleware>();

app.Use(async (context, next) =>
{
    Stopwatch stopwatch = Stopwatch.StartNew();
    await next(context);

    IReverseProxyFeature? proxy = context.Features.Get<IReverseProxyFeature>();
    IForwarderErrorFeature? error = context.Features.Get<IForwarderErrorFeature>();
    if (error is not null)
    {
        app.Logger.LogWarning(
            error.Exception,
            "{Method} {Path} -> route {Route}: destination failed with {ForwarderError}",
            context.Request.Method,
            context.Request.Path,
            proxy?.Route.Config.RouteId,
            error.Error
        );
    }

    app.Logger.LogInformation(
        "{Method} {Path} from {ClientIp} -> route {Route}, cluster {Cluster}, destination {Destination}: {StatusCode} in {ElapsedMilliseconds} ms",
        context.Request.Method,
        context.Request.Path,
        context.Connection.RemoteIpAddress,
        proxy?.Route.Config.RouteId,
        proxy?.Cluster.Config.ClusterId,
        proxy?.ProxiedDestination?.Model.Config.Address,
        context.Response.StatusCode,
        stopwatch.ElapsedMilliseconds
    );
});

app.UseAuthentication();
app.UseAuthorization();

app.MapReverseProxy();
app.MapDefaultEndpoints();

await app.RunAsync();
