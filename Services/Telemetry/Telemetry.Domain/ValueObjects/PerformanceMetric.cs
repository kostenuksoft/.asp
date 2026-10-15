using Telemetry.Domain.Common;
using Telemetry.Domain.Exceptions;

namespace Telemetry.Domain.ValueObjects;

public sealed class PerformanceMetric : ValueObject
{
    public PerformanceMetric(
        string name,
        double observed,
        double threshold,
        string? unit
    )
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainException("A metric needs its name.");
        }

        if (!double.IsFinite(observed) || !double.IsFinite(threshold) || threshold <= 0)
        {
            throw new DomainException("Observed value and threshold must be finite, the threshold positive.");
        }

        if (observed <= threshold)
        {
            throw new DomainException($"Metric '{name}' is not degraded: observed value must exceed the threshold.");
        }

        Name = name;
        Observed = observed;
        Threshold = threshold;
        Unit = unit;
    }

    public string Name { get; }

    public double Observed { get; }

    public double Threshold { get; }

    public string? Unit { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Name;
        yield return Observed;
        yield return Threshold;
        yield return Unit;
    }
}
