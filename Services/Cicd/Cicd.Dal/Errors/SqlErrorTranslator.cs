using Cicd.Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace Cicd.Dal.Errors;

public static class SqlErrorTranslator
{
    private static readonly HashSet<int> s_notFound = [50001, 50010, 50020, 50030, 50040, 50050];
    private static readonly HashSet<int> s_conflict = [50002, 50011, 50021, 50022, 50031, 50051, 50052, 2601, 2627];
    private static readonly HashSet<int> s_invalid = [547, 515, 8152, 2628];

    public static bool TryTranslate(
        SqlException exception,
        out DomainException translated
    )
    {
        ArgumentNullException.ThrowIfNull(exception);

        if (s_notFound.Contains(exception.Number))
        {
            translated = new NotFoundException(exception.Message, exception);
            return true;
        }

        if (s_conflict.Contains(exception.Number))
        {
            translated = exception.Number is 2601 or 2627
                ? new BusinessConflictException("A record with the same unique key already exists.", exception)
                : new BusinessConflictException(exception.Message, exception);
            return true;
        }

        if (s_invalid.Contains(exception.Number))
        {
            translated = new ValidationException("The data violates a database constraint.", exception);
            return true;
        }

        translated = null!;
        return false;
    }
}
