namespace Cicd.Domain.Exceptions;

public sealed class ValidationException : DomainException
{
    public ValidationException(string message)
        : base(message)
    {
    }

    public ValidationException(
        string message,
        Exception innerException
    )
        : base(message, innerException)
    {
    }
}
