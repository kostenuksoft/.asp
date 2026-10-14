using MediatR;
using Microsoft.Extensions.Logging;

namespace Telemetry.Application.Common.Behaviors;

public sealed class LoggingBehavior<TRequest, TResponse>(
    ILogger<LoggingBehavior<TRequest, TResponse>> logger
)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(next);

        string requestName = typeof(TRequest).Name;
        logger.LogInformation("Handling {RequestName} {@Request}", requestName, request);

        try
        {
            TResponse response = await next(cancellationToken);
            logger.LogInformation("Handled {RequestName} {@Response}", requestName, response);
            return response;
        }
        catch (Exception exception) when (LogFailure(requestName, exception))
        {
            throw;
        }
    }

    private bool LogFailure(
        string requestName,
        Exception exception
    )
    {
        logger.LogWarning(
            "{RequestName} failed with {ExceptionType}: {ExceptionMessage}",
            requestName,
            exception.GetType().Name,
            exception.Message
        );
        return false;
    }
}
