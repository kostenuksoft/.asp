using Grpc.Core;
using Grpc.Health.V1;
using Grpc.Net.ClientFactory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Platform.Aggregator.HealthChecks;

public sealed class GrpcDependencyHealthCheck(
    GrpcClientFactory clientFactory,
    ILogger<GrpcDependencyHealthCheck> logger,
    string service
)
    : IHealthCheck
{
    private static readonly TimeSpan s_deadline = TimeSpan.FromSeconds(2);

    public async Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(context);

        Health.HealthClient client = clientFactory.CreateClient<Health.HealthClient>(service);
        try
        {
            HealthCheckResponse reply = await client.CheckAsync(
                new HealthCheckRequest(),
                deadline: DateTime.UtcNow.Add(s_deadline),
                cancellationToken: cancellationToken
            );
            return reply.Status == HealthCheckResponse.Types.ServingStatus.Serving
                ? HealthCheckResult.Healthy($"{service} is serving")
                : new HealthCheckResult(context.Registration.FailureStatus, $"{service} status: {reply.Status}");
        }
        catch (RpcException exception)
        {
            logger.LogWarning(
                exception,
                "{Service} health check failed with {GrpcStatus}",
                service,
                exception.StatusCode
            );
            return new HealthCheckResult(context.Registration.FailureStatus, $"{service} is unavailable", exception);
        }
    }
}
