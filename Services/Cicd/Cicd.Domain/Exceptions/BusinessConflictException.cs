namespace Cicd.Domain.Exceptions;

public sealed class BusinessConflictException : DomainException
{
    public BusinessConflictException(string message)
        : base(message)
    {
    }

    public BusinessConflictException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
