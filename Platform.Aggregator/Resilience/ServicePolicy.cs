namespace Platform.Aggregator.Resilience;

public sealed record ServicePolicy(
    string Name,
    int GrpcMaxAttempts,
    TimeSpan GrpcInitialBackoff,
    TimeSpan GrpcMaxBackoff,
    int HttpRetryAttempts,
    TimeSpan HttpRetryDelay,
    TimeSpan AttemptTimeout,
    TimeSpan TotalTimeout,
    double FailureRatio,
    int MinimumThroughput,
    TimeSpan SamplingDuration,
    TimeSpan BreakDuration,
    TimeSpan CallDeadline
)
{
    public static readonly ServicePolicy Critical = new(
        Name: "critical",
        GrpcMaxAttempts: 4,
        GrpcInitialBackoff: TimeSpan.FromMilliseconds(200),
        GrpcMaxBackoff: TimeSpan.FromSeconds(2),
        HttpRetryAttempts: 2,
        HttpRetryDelay: TimeSpan.FromMilliseconds(200),
        AttemptTimeout: TimeSpan.FromSeconds(2),
        TotalTimeout: TimeSpan.FromSeconds(7),
        FailureRatio: 0.3,
        MinimumThroughput: 5,
        SamplingDuration: TimeSpan.FromSeconds(10),
        BreakDuration: TimeSpan.FromSeconds(15),
        CallDeadline: TimeSpan.FromSeconds(10)
    );

    public static readonly ServicePolicy NonCritical = new(
        Name: "non-critical",
        GrpcMaxAttempts: 2,
        GrpcInitialBackoff: TimeSpan.FromMilliseconds(200),
        GrpcMaxBackoff: TimeSpan.FromMilliseconds(500),
        HttpRetryAttempts: 1,
        HttpRetryDelay: TimeSpan.FromMilliseconds(200),
        AttemptTimeout: TimeSpan.FromSeconds(1),
        TotalTimeout: TimeSpan.FromMilliseconds(2500),
        FailureRatio: 0.7,
        MinimumThroughput: 5,
        SamplingDuration: TimeSpan.FromSeconds(10),
        BreakDuration: TimeSpan.FromSeconds(30),
        CallDeadline: TimeSpan.FromSeconds(3)
    );
}
