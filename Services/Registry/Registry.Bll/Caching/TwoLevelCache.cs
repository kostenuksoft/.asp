using System.Text.Json;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using StackExchange.Redis;

namespace Registry.Bll.Caching;

public sealed class TwoLevelCache(
    IMemoryCache memory,
    IDistributedCache redis,
    CacheMetrics metrics,
    ILogger<TwoLevelCache> logger
)
{
    private const string MemoryLevel = "L1";
    private const string RedisLevel = "L2";

    private static readonly JsonSerializerOptions s_json = new(JsonSerializerDefaults.Web);

    public async Task<T> GetOrCreateAsync<T>(
        string key,
        CacheEntryPolicy policy,
        Func<CancellationToken, Task<T>> factory,
        CancellationToken cancellationToken
    )
        where T : class
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(factory);
        ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(policy.MemoryAbsolute, policy.RedisAbsolute);

        if (memory.TryGetValue(key, out T? cached) && cached is not null)
        {
            metrics.Hit(MemoryLevel);
            logger.LogDebug("Cache hit {CacheKey} at {CacheLevel}", key, MemoryLevel);
            return cached;
        }

        metrics.Miss(MemoryLevel);

        byte[]? stored = await ReadRedisAsync(key, cancellationToken);
        T? shared = stored is null ? null : JsonSerializer.Deserialize<T>(stored, s_json);
        if (shared is not null)
        {
            metrics.Hit(RedisLevel);
            logger.LogDebug(
                "Cache hit {CacheKey} at {CacheLevel}, {SizeBytes} bytes, copied to L1 for {MemoryTtl}",
                key,
                RedisLevel,
                stored!.Length,
                policy.MemoryAbsolute
            );
            SetMemory(key, shared, policy);
            return shared;
        }

        metrics.Miss(RedisLevel);
        logger.LogDebug("Cache miss {CacheKey}, reading the database", key);

        T value = await factory(cancellationToken);
        byte[] payload = JsonSerializer.SerializeToUtf8Bytes(value, s_json);

        await WriteRedisAsync(key, payload, policy, cancellationToken);
        SetMemory(key, value, policy);

        logger.LogDebug(
            "Cache filled {CacheKey}, {SizeBytes} bytes, L1 {MemoryTtl}, L2 {RedisTtl}",
            key,
            payload.Length,
            policy.MemoryAbsolute,
            policy.RedisAbsolute
        );
        return value;
    }

    public async Task RemoveAsync(
        string key,
        string reason,
        CancellationToken cancellationToken
    )
    {
        memory.Remove(key);
        try
        {
            await redis.RemoveAsync(key, cancellationToken);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            logger.LogWarning(exception, "Redis unavailable, {CacheKey} removed from L1 only", key);
        }

        metrics.Invalidated(reason);
        logger.LogInformation("Cache invalidated {CacheKey}, reason {Reason}", key, reason);
    }

    public async Task<string?> GetVersionAsync(
        string key,
        CancellationToken cancellationToken
    )
    {
        try
        {
            string? version = await redis.GetStringAsync(key, cancellationToken);
            if (version is not null)
            {
                return version;
            }

            version = NewVersion();
            await redis.SetStringAsync(key, version, cancellationToken);
            return version;
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            logger.LogWarning(exception, "Redis unavailable, version {CacheKey} unknown, lists read from the database", key);
            return null;
        }
    }

    public async Task RenewVersionAsync(
        string key,
        string reason,
        CancellationToken cancellationToken
    )
    {
        try
        {
            await redis.SetStringAsync(key, NewVersion(), cancellationToken);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            logger.LogWarning(exception, "Redis unavailable, version {CacheKey} not renewed", key);
            return;
        }

        metrics.Invalidated(reason);
        logger.LogInformation("Cache invalidated {CacheKey}, reason {Reason}", key, reason);
    }

    private void SetMemory<T>(
        string key,
        T value,
        CacheEntryPolicy policy
    )
    {
        MemoryCacheEntryOptions options = new()
        {
            Size = 1,
            SlidingExpiration = policy.MemorySliding,
            AbsoluteExpirationRelativeToNow = policy.MemoryAbsolute
        };
        memory.Set(key, value, options);
    }

    private async Task<byte[]?> ReadRedisAsync(
        string key,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await redis.GetAsync(key, cancellationToken);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            logger.LogWarning(exception, "Redis unavailable, {CacheKey} read as an L2 miss", key);
            return null;
        }
    }

    private async Task WriteRedisAsync(
        string key,
        byte[] payload,
        CacheEntryPolicy policy,
        CancellationToken cancellationToken
    )
    {
        DistributedCacheEntryOptions options = new()
        {
            AbsoluteExpirationRelativeToNow = policy.RedisAbsolute
        };
        try
        {
            await redis.SetAsync(key, payload, options, cancellationToken);
        }
        catch (Exception exception) when (IsRedisFailure(exception))
        {
            logger.LogWarning(exception, "Redis unavailable, {CacheKey} kept in L1 only", key);
        }
    }

    private static string NewVersion() => Guid.NewGuid().ToString("N");

    private static bool IsRedisFailure(Exception exception) => exception is RedisException or RedisTimeoutException;
}
