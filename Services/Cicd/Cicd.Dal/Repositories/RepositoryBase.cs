using System.Data;
using Cicd.Dal.Errors;
using Cicd.Domain.Exceptions;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Cicd.Dal.Repositories;

public abstract class RepositoryBase(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
{
    public const int CommandTimeoutSeconds = 30;

    protected SqlConnection Connection { get; } = connection;

    protected SqlTransaction? Transaction => currentTransaction();

    protected CommandDefinition Command(
        string sql,
        object? parameters,
        CancellationToken cancellationToken
    ) => new(sql, parameters, Transaction, CommandTimeoutSeconds, cancellationToken: cancellationToken);

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
