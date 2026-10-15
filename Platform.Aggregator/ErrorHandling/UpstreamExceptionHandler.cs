using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Platform.Aggregator.Services;

namespace Platform.Aggregator.ErrorHandling;

public sealed class UpstreamExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<UpstreamExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(httpContext);
        ArgumentNullException.ThrowIfNull(exception);

        bool unavailable = BuildRunCardService.IsUnavailable(exception, httpContext.RequestAborted);
        int status = unavailable ? StatusCodes.Status503ServiceUnavailable : StatusCodes.Status500InternalServerError;
        if (!unavailable)
        {
            logger.LogError(exception, "Unhandled exception for {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails
            {
                Status = status,
                Title = unavailable ? "Source service unavailable" : "Unexpected server error",
                Detail = unavailable ? "cicd-api, the owner of the build run, did not answer." : null,
            },
        });
    }
}
