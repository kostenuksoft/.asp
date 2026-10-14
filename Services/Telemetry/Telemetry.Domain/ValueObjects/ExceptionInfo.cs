using Telemetry.Domain.Common;
using Telemetry.Domain.Exceptions;

namespace Telemetry.Domain.ValueObjects;

public sealed class ExceptionInfo : ValueObject
{
    public const int MaxTypeLength = 300;

    public ExceptionInfo(
        string type,
        string message,
        string? innerType
    )
    {
        if (string.IsNullOrWhiteSpace(type) || type.Length > MaxTypeLength)
        {
            throw new DomainException($"Exception type is required and has at most {MaxTypeLength} characters.");
        }

        if (string.IsNullOrWhiteSpace(message))
        {
            throw new DomainException("Exception message is required.");
        }

        if (innerType is not null && (string.IsNullOrWhiteSpace(innerType) || innerType.Length > MaxTypeLength))
        {
            throw new DomainException($"Inner exception type, when given, has 1 to {MaxTypeLength} characters.");
        }

        Type = type;
        Message = message;
        InnerType = innerType;
    }

    public string Type { get; }

    public string Message { get; }

    public string? InnerType { get; }

    protected override IEnumerable<object?> GetEqualityComponents()
    {
        yield return Type;
        yield return Message;
        yield return InnerType;
    }
}
