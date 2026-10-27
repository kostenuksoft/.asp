using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Serilog.Context;

namespace Platform.ServiceDefaults;

public sealed class CorrelationIdMiddleware(
    RequestDelegate next,
    ILogger<CorrelationIdMiddleware> logger
)
{
    public const string HeaderName = "X-Correlation-Id";

    public async Task InvokeAsync(HttpContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        string? correlationId = context.Request.Headers[HeaderName];
        if (string.IsNullOrEmpty(correlationId))
        {
            correlationId = Guid.NewGuid().ToString();
            context.Request.Headers[HeaderName] = correlationId;
            logger.LogInformation(
                "Generated correlation id {CorrelationId} for {Method} {Path}",
                correlationId,
                context.Request.Method,
                context.Request.Path
            );
        }

        context.Response.OnStarting(() =>
        {
            context.Response.Headers[HeaderName] = correlationId;
            return Task.CompletedTask;
        });
        Activity.Current?.SetTag("correlation.id", correlationId);

        using (LogContext.PushProperty("CorrelationId", correlationId))
        {
            logger.LogInformation("Request started {Method} {Path}", context.Request.Method, context.Request.Path);
            await next(context);
            logger.LogInformation("Request finished with {StatusCode}", context.Response.StatusCode);
        }
    }
}
