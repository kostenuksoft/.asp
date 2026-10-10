using System.Data;
using Cicd.Dal.Errors;
using Cicd.Domain.Exceptions;
using Microsoft.Data.SqlClient;

namespace Cicd.Dal.Repositories;

public abstract class RepositoryBase
{
    public const int CommandTimeoutSeconds = 30;

    private readonly Func<SqlTransaction?> _currentTransaction;

    protected RepositoryBase(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
    {
        Connection = connection;
        _currentTransaction = currentTransaction;
    }

    protected SqlConnection Connection { get; }

    protected SqlTransaction? Transaction => _currentTransaction();

    protected async Task EnsureOpenAsync(CancellationToken cancellationToken)
    {
        if (Connection.State != ConnectionState.Open)
        {
            await Connection.OpenAsync(cancellationToken);
        }
    }

    protected static async Task<TResult> TranslateErrorsAsync<TResult>(Func<Task<TResult>> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            return await operation();
        }
        catch (SqlException exception) when (SqlErrorTranslator.TryTranslate(exception, out DomainException translated))
        {
            throw translated;
        }
    }

    protected static async Task TranslateErrorsAsync(Func<Task> operation)
    {
        ArgumentNullException.ThrowIfNull(operation);

        try
        {
            await operation();
        }
        catch (SqlException exception) when (SqlErrorTranslator.TryTranslate(exception, out DomainException translated))
        {
            throw translated;
        }
    }
}
