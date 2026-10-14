using Telemetry.Domain.Exceptions;
using Telemetry.Domain.ValueObjects;
using Xunit;

namespace Telemetry.Tests.Domain;

public sealed class ValueObjectTests
{
    [Fact]
    public void ValueObjectsWithEqualComponentsAreEqual()
    {
        ExceptionInfo first = new("System.TimeoutException", "The operation timed out.", null);
        ExceptionInfo second = new("System.TimeoutException", "The operation timed out.", null);

        Assert.Equal(first, second);
        Assert.Equal(first.GetHashCode(), second.GetHashCode());
        Assert.NotSame(first, second);
    }

    [Fact]
    public void ValueObjectsWithDifferentComponentsDiffer()
    {
        StackFrame first = new("ClaimValidator.Validate", "ClaimValidator.cs", 88);
        StackFrame second = new("ClaimValidator.Validate", "ClaimValidator.cs", 89);

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void MetricWithinThresholdIsNotADegradation()
    {
        Assert.Throws<DomainException>(() => new PerformanceMetric("blobUploadLatencyP95", 900, 1200, "ms"));
    }
}
