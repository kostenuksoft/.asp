using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using OpenTelemetry;
using OpenTelemetry.Metrics;
using OpenTelemetry.Trace;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;

namespace Platform.ServiceDefaults;

public static class Extensions
{
    private const string HealthEndpointPath = "/health";
    private const string LivenessEndpointPath = "/health/live";
    private const string ReadinessEndpointPath = "/health/ready";
    private const string LiveTag = "live";
    private const string ReadyTag = "ready";
    private const string MongoDbActivitySource = "MongoDB.Driver.Core.Extensions.DiagnosticSources";
    private const string SamplingRatioKey = "OpenTelemetry:SamplingRatio";
    private const string RegistryCacheMeter = "Registry.Cache";
    private const string PollyMeter = "Polly";

    public static TBuilder AddServiceDefaults<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.ClearProviders();
        builder.Services.AddSerilog(
            (services, configuration) => configuration
                .MinimumLevel.Is(builder.Environment.IsDevelopment() ? LogEventLevel.Debug : LogEventLevel.Information)
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .MinimumLevel.Override("Microsoft.EntityFrameworkCore", LogEventLevel.Warning)
                .MinimumLevel.Override("System.Net.Http.HttpClient", LogEventLevel.Warning)
                .MinimumLevel.Override("Polly", LogEventLevel.Warning)
                .ReadFrom.Configuration(builder.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .Enrich.WithProperty("ServiceName", builder.Environment.ApplicationName)
                .Enrich.WithEnvironmentName()
                .Enrich.WithMachineName()
                .Enrich.WithThreadId()
                .WriteTo.Console(new CompactJsonFormatter()),
            writeToProviders: true
        );

        builder.ConfigureOpenTelemetry();
        builder.AddDefaultHealthChecks();

        builder.Services.AddServiceDiscovery();
        builder.Services.AddHttpContextAccessor();
        builder.Services.AddTransient<CorrelationIdHandler>();
        builder.Services.ConfigureHttpClientDefaults(http =>
        {
            http.AddStandardResilienceHandler();
            http.AddServiceDiscovery();
            http.AddHttpMessageHandler<CorrelationIdHandler>();
        });

        return builder;
    }

    public static TBuilder ConfigureOpenTelemetry<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Logging.AddOpenTelemetry(logging =>
        {
            logging.IncludeFormattedMessage = true;
            logging.IncludeScopes = true;
        });

        double samplingRatio = builder.Environment.IsDevelopment()
            ? 1.0
            : builder.Configuration.GetValue(SamplingRatioKey, 1.0);

        builder.Services.AddOpenTelemetry()
            .WithMetrics(metrics => metrics
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation()
                .AddRuntimeInstrumentation()
                .AddMeter(builder.Environment.ApplicationName, RegistryCacheMeter, PollyMeter)
            )
            .WithTracing(tracing => tracing
                .SetSampler(new ParentBasedSampler(new TraceIdRatioBasedSampler(samplingRatio)))
                .AddSource(builder.Environment.ApplicationName)
                .AddSource(MongoDbActivitySource)
                .AddSource("Platform.Messaging")
                .AddAspNetCoreInstrumentation(options =>
                    options.Filter = context => !IsHealthRequest(context.Request.Path)
                )
                .AddHttpClientInstrumentation()
                .AddSqlClientInstrumentation()
            );

        if (!string.IsNullOrWhiteSpace(builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"]))
        {
            builder.Services.AddOpenTelemetry().UseOtlpExporter();
        }

        return builder;
    }

    public static TBuilder AddDefaultHealthChecks<TBuilder>(this TBuilder builder)
        where TBuilder : IHostApplicationBuilder
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.Services.AddHealthChecks()
            .AddCheck("self", () => HealthCheckResult.Healthy(), [LiveTag]);

        return builder;
    }

    public static WebApplication MapDefaultEndpoints(this WebApplication app)
    {
        ArgumentNullException.ThrowIfNull(app);

        app.MapHealthChecks(HealthEndpointPath, new HealthCheckOptions
        {
            ResponseWriter = WriteHealthReportAsync,
        });
        app.MapHealthChecks(LivenessEndpointPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(LiveTag),
        });
        app.MapHealthChecks(ReadinessEndpointPath, new HealthCheckOptions
        {
            Predicate = registration => registration.Tags.Contains(ReadyTag),
            ResponseWriter = WriteHealthReportAsync,
        });

        return app;
    }

    private static Task WriteHealthReportAsync(
        HttpContext context,
        HealthReport report
    )
    {
        var payload = new
        {
            status = report.Status.ToString(),
            totalDurationMs = Math.Round(report.TotalDuration.TotalMilliseconds, 1),
            checks = report.Entries.Select(entry => new
            {
                name = entry.Key,
                status = entry.Value.Status.ToString(),
                description = entry.Value.Description,
                durationMs = Math.Round(entry.Value.Duration.TotalMilliseconds, 1),
                tags = entry.Value.Tags,
                data = entry.Value.Data,
            }),
        };
        return context.Response.WriteAsJsonAsync(payload);
    }

    private static bool IsHealthRequest(PathString path) => path.StartsWithSegments(HealthEndpointPath);
}
