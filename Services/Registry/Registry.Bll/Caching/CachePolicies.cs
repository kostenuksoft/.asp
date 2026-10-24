namespace Registry.Bll.Caching;

public static class CachePolicies
{
    public static readonly CacheEntryPolicy PackageCard = new(
        MemorySliding: TimeSpan.FromMinutes(1),
        MemoryAbsolute: TimeSpan.FromMinutes(2),
        RedisAbsolute: TimeSpan.FromMinutes(10)
    );

    public static readonly CacheEntryPolicy Tags = new(
        MemorySliding: null,
        MemoryAbsolute: TimeSpan.FromMinutes(5),
        RedisAbsolute: TimeSpan.FromHours(1)
    );

    public static readonly CacheEntryPolicy PackageList = new(
        MemorySliding: null,
        MemoryAbsolute: TimeSpan.FromSeconds(30),
        RedisAbsolute: TimeSpan.FromMinutes(2)
    );
}
