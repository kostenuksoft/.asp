using System.Diagnostics.Metrics;

namespace Registry.Bll.Caching;

public sealed class CacheMetrics
{
    public const string MeterName = "Registry.Cache";

    private readonly Counter<long> _hits;
    private readonly Counter<long> _misses;
    private readonly Counter<long> _invalidations;

    public CacheMetrics(IMeterFactory meterFactory)
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        Meter meter = meterFactory.Create(MeterName);
        _hits = meter.CreateCounter<long>("cache.hits", description: "Reads answered by a cache level");
        _misses = meter.CreateCounter<long>("cache.misses", description: "Reads a cache level could not answer");
        _invalidations = meter.CreateCounter<long>(
            "cache.invalidations",
            description: "Keys removed or replaced after a write"
        );
    }

    public void Hit(string level) => _hits.Add(1, new KeyValuePair<string, object?>("cache.level", level));

    public void Miss(string level) => _misses.Add(1, new KeyValuePair<string, object?>("cache.level", level));

    public void Invalidated(string reason) => _invalidations.Add(
        1,
        new KeyValuePair<string, object?>("cache.reason", reason)
    );
}
