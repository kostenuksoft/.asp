using Telemetry.Domain.Common;
using Telemetry.Domain.Exceptions;

namespace Telemetry.Domain.ValueObjects;

public sealed class StackFrame : ValueObject
{
    public StackFrame(
        string method,
        string? file,
        int? line
    )
    {
        if (string.IsNullOrWhiteSpace(method))
        {
            throw new DomainException("A stack frame needs its method.");
        }

        if (line < 0)
        {
            throw new DomainException("A stack frame line cannot be negative.");
        }

        Method = method;
        File = file;
        Line = line;
    }

    public string Method { get; }

    public string? File { get; }

    public int? Line { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Method;
        yield return File;
        yield return Line;
    }
}
