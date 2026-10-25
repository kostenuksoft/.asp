using Cicd.Dal.Repositories.Interfaces;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Cicd.Dal.Repositories;

public sealed class ProcessedEventRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
    : RepositoryBase(connection, currentTransaction), IProcessedEventRepository
{
    private const int PrimaryKeyViolation = 2627;
    private const int UniqueIndexViolation = 2601;

    public async Task<bool> TryAddAsync(
        Guid eventId,
        string eventType,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            await Connection.ExecuteAsync(Command(
                "INSERT INTO cicd.ProcessedEvent (EventId, EventType) VALUES (@EventId, @EventType);",
                new { EventId = eventId, EventType = eventType },
                cancellationToken
            ));
            return true;
        }
        catch (SqlException exception) when (exception.Number is PrimaryKeyViolation or UniqueIndexViolation)
        {
            return false;
        }
    }
}
