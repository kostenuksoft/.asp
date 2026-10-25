using System.Data;
using Cicd.Dal.Repositories.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Cicd.Dal.Repositories;

public abstract class BaseDapperRepository<T>(
    SqlConnection connection,
    Func<SqlTransaction?> currentTransaction,
    string tableName,
    string keyColumn
)
    : RepositoryBase(connection, currentTransaction), IGenericRepository<T>
    where T : class
{
    private readonly string _selectByIdSql = $"SELECT * FROM {tableName} WHERE {keyColumn} = @Id AND IsDeleted = 0;";
    private readonly string _selectAllSql = $"SELECT * FROM {tableName} WHERE IsDeleted = 0 ORDER BY {keyColumn};";
    private readonly string _softDeleteSql =
        $"UPDATE {tableName} SET IsDeleted = 1, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = SUSER_SNAME() " +
        $"WHERE {keyColumn} = @Id AND IsDeleted = 0;";

    protected abstract string InsertSql { get; }

    public virtual Task<T?> GetByIdAsync(
        long id,
        CancellationToken cancellationToken = default
    ) => Connection.QuerySingleOrDefaultAsync<T>(Command(_selectByIdSql, new { Id = id }, cancellationToken));

    public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        IEnumerable<T> rows = await Connection.QueryAsync<T>(Command(_selectAllSql, null, cancellationToken));
        return rows.AsList();
    }

    public virtual Task<long> AddAsync(
        T entity,
        CancellationToken cancellationToken = default
    ) => TranslateErrorsAsync(() => Connection.ExecuteScalarAsync<long>(Command(InsertSql, entity, cancellationToken)));

    public virtual async Task<bool> DeleteAsync(
        long id,
        CancellationToken cancellationToken = default
    )
    {
        int affected = await Connection.ExecuteAsync(Command(_softDeleteSql, new { Id = id }, cancellationToken));
        return affected == 1;
    }

    protected CommandDefinition Procedure(
        string name,
        object? parameters,
        CancellationToken cancellationToken
    ) => new(name, parameters, Transaction, CommandTimeoutSeconds, CommandType.StoredProcedure, cancellationToken: cancellationToken);
}
