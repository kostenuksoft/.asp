using System.Diagnostics;
using MediatR;
using Microsoft.Extensions.Logging;

namespace Telemetry.Application.Common.Behaviors;

public sealed class PerformanceBehavior<TRequest, TResponse>(
    ILogger<PerformanceBehavior<TRequest, TResponse>> logger
)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : notnull
{
    private const long SlowRequestMilliseconds = 500;

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(next);

        Stopwatch stopwatch = Stopwatch.StartNew();
        TResponse response = await next(cancellationToken);
        stopwatch.Stop();

        if (stopwatch.ElapsedMilliseconds > SlowRequestMilliseconds)
        {
            logger.LogWarning(
                "Slow request {RequestName}: {ElapsedMilliseconds} ms",
                typeof(TRequest).Name,
                stopwatch.ElapsedMilliseconds
            );
        }

        return response;
    }
}
