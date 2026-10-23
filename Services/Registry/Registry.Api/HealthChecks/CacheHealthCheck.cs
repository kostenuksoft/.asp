using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Registry.Api.HealthChecks;

public sealed class CacheHealthCheck(IMemoryCache memory) : IHealthCheck
{
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default
    )
    {
        MemoryCacheStatistics? statistics = (memory as MemoryCache)?.GetCurrentStatistics();
        if (statistics is null)
        {
            return Task.FromResult(HealthCheckResult.Healthy("Memory cache statistics are not tracked"));
        }

        Dictionary<string, object> data = new()
        {
            ["entries"] = statistics.CurrentEntryCount,
            ["size"] = statistics.CurrentEstimatedSize ?? 0,
            ["hits"] = statistics.TotalHits,
            ["misses"] = statistics.TotalMisses
        };
        return Task.FromResult(HealthCheckResult.Healthy("Memory cache statistics", data));
    }
}
