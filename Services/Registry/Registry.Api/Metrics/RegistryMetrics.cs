using System.Diagnostics.Metrics;
using Microsoft.Extensions.Caching.Memory;

namespace Registry.Api.Metrics;

public sealed class RegistryMetrics
{
    public const string MeterName = "Registry.Api";

    private readonly Counter<long> _packagesCreated;
    private readonly Counter<long> _licensePricesChanged;
    private readonly Histogram<double> _operationDuration;

    public RegistryMetrics(
        IMeterFactory meterFactory,
        IMemoryCache memory
    )
    {
        ArgumentNullException.ThrowIfNull(meterFactory);

        Meter meter = meterFactory.Create(MeterName);
        _packagesCreated = meter.CreateCounter<long>(
            "packages.created",
            unit: "{package}",
            description: "Packages published to the registry"
        );
        _licensePricesChanged = meter.CreateCounter<long>(
            "packages.license_price_changed",
            unit: "{change}",
            description: "Licence price changes"
        );
        _operationDuration = meter.CreateHistogram<double>(
            "packages.operation.duration",
            unit: "ms",
            description: "Duration of package write operations"
        );
        meter.CreateObservableGauge(
            "registry.cache.entries",
            () => (memory as MemoryCache)?.GetCurrentStatistics()?.CurrentEntryCount ?? 0,
            unit: "{entry}",
            description: "Entries currently held in the L1 memory cache"
        );
    }

    public void PackageCreated(decimal licensePrice) => _packagesCreated.Add(1, LicenseKind(licensePrice));

    public void LicensePriceChanged(decimal licensePrice) => _licensePricesChanged.Add(1, LicenseKind(licensePrice));

    public void RecordDuration(
        string operation,
        bool success,
        double milliseconds
    ) => _operationDuration.Record(
        milliseconds,
        new KeyValuePair<string, object?>("operation", operation),
        new KeyValuePair<string, object?>("status", success ? "success" : "failure")
    );

    private static KeyValuePair<string, object?> LicenseKind(decimal licensePrice) =>
        new("license.kind", licensePrice == 0 ? "free" : "commercial");
}
