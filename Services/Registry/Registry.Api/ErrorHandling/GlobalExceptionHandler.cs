using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Registry.Domain.Exceptions;

namespace Registry.Api.ErrorHandling;

public sealed class GlobalExceptionHandler(
    IProblemDetailsService problemDetailsService,
    IHostEnvironment environment,
    ILogger<GlobalExceptionHandler> logger
)
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

        (int status, string title) = exception switch
        {
            EntityNotFoundException => (StatusCodes.Status404NotFound, "Resource not found"),
            ValidationException => (StatusCodes.Status400BadRequest, "Validation failed"),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict with the current state"),
            DomainException => (StatusCodes.Status409Conflict, "Business rule violated"),
            _ => (StatusCodes.Status500InternalServerError, "Unexpected server error"),
        };

        LogLevel level = status == StatusCodes.Status500InternalServerError ? LogLevel.Error : LogLevel.Warning;
        logger.Log(
            level,
            exception,
            "Request {Method} {Path} failed with {StatusCode}",
            httpContext.Request.Method,
            httpContext.Request.Path,
            status
        );

        ProblemDetails problemDetails = new()
        {
            Status = status,
            Title = title,
        };

        if (environment.IsDevelopment())
        {
            problemDetails.Detail = exception.Message;
            problemDetails.Extensions["exceptionType"] = exception.GetType().Name;
        }

        httpContext.Response.StatusCode = status;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = problemDetails,
        });
    }
}
