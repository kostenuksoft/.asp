namespace Registry.Bll.Caching;

public sealed record CacheEntryPolicy(
    TimeSpan? MemorySliding,
    TimeSpan MemoryAbsolute,
    TimeSpan RedisAbsolute
);
