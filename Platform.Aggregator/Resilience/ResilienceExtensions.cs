using Grpc.Core;
using Grpc.Net.Client.Configuration;
using Microsoft.Extensions.Http.Resilience;
using Polly;

namespace Platform.Aggregator.Resilience;

public static class ResilienceExtensions
{
    public static IHttpClientBuilder WithGrpcRetry(
        this IHttpClientBuilder client,
        ServicePolicy policy
    )
    {
        ArgumentNullException.ThrowIfNull(policy);

        return client.ConfigureChannel(channel => channel.ServiceConfig = new ServiceConfig
        {
            MethodConfigs =
            {
                new MethodConfig
                {
                    Names = { MethodName.Default },
                    RetryPolicy = new RetryPolicy
                    {
                        MaxAttempts = policy.GrpcMaxAttempts,
                        InitialBackoff = policy.GrpcInitialBackoff,
                        MaxBackoff = policy.GrpcMaxBackoff,
                        BackoffMultiplier = 2,
                        RetryableStatusCodes = { StatusCode.Unavailable, StatusCode.ResourceExhausted },
                    },
                },
            },
        });
    }

    public static IHttpClientBuilder WithServicePolicy(
        this IHttpClientBuilder client,
        string service,
        ServicePolicy policy
    )
    {
        ArgumentNullException.ThrowIfNull(policy);

        client.RemoveAllResilienceHandlers();
        client.AddStandardResilienceHandler().Configure((options, provider) =>
        {
            ILogger logger = provider.GetRequiredService<ILoggerFactory>().CreateLogger("Platform.Aggregator.Resilience");

            options.Retry.MaxRetryAttempts = policy.HttpRetryAttempts;
            options.Retry.Delay = policy.HttpRetryDelay;
            options.Retry.BackoffType = DelayBackoffType.Exponential;
            options.Retry.UseJitter = true;
            options.Retry.DisableFor(HttpMethod.Put, HttpMethod.Patch, HttpMethod.Delete);

            options.CircuitBreaker.FailureRatio = policy.FailureRatio;
            options.CircuitBreaker.MinimumThroughput = policy.MinimumThroughput;
            options.CircuitBreaker.SamplingDuration = policy.SamplingDuration;
            options.CircuitBreaker.BreakDuration = policy.BreakDuration;
            options.CircuitBreaker.OnOpened = arguments =>
            {
                logger.LogError(
                    "Circuit for {Service} ({Policy}) opened for {BreakDuration}",
                    service,
                    policy.Name,
                    arguments.BreakDuration
                );
                return ValueTask.CompletedTask;
            };
            options.CircuitBreaker.OnHalfOpened = _ =>
            {
                logger.LogInformation("Circuit for {Service} half-opened, sending a trial call", service);
                return ValueTask.CompletedTask;
            };
            options.CircuitBreaker.OnClosed = _ =>
            {
                logger.LogInformation("Circuit for {Service} closed", service);
                return ValueTask.CompletedTask;
            };

            options.AttemptTimeout.Timeout = policy.AttemptTimeout;
            options.TotalRequestTimeout.Timeout = policy.TotalTimeout;
        });

        return client;
    }
}
