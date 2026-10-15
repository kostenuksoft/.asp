using System.Diagnostics;
using Platform.ServiceDefaults;
using Yarp.ReverseProxy.Forwarder;
using Yarp.ReverseProxy.Model;

WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

builder.AddServiceDefaults();

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

app.MapReverseProxy();
app.MapDefaultEndpoints();

await app.RunAsync();
