using System.Data;
using Cicd.Dal.Repositories.Interfaces;
using Cicd.Domain.Models;
using Microsoft.Data.SqlClient;

namespace Cicd.Dal.Repositories;

public sealed class AgentRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
    : RepositoryBase(connection, currentTransaction), IAgentRepository
{
    private const string SelectColumns =
        "SELECT AgentId, Hostname, OperatingSystem, Status, LastHeartbeatAt, CreatedAt, CreatedBy, UpdatedAt, UpdatedBy, IsDeleted " +
        "FROM cicd.Agent ";

    public async Task<Agent?> GetByIdAsync(
        long agentId,
        CancellationToken cancellationToken = default
    )
    {
        const string Sql = SelectColumns + "WHERE AgentId = @AgentId AND IsDeleted = 0;";

        await EnsureOpenAsync(cancellationToken);
        await using SqlCommand command = new(Sql, Connection, Transaction);
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.Add("@AgentId", SqlDbType.BigInt).Value = agentId;

        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        return await reader.ReadAsync(cancellationToken) ? Map(reader) : null;
    }

    public async Task<IReadOnlyList<Agent>> GetByStatusAsync(
        string status,
        CancellationToken cancellationToken = default
    )
    {
        const string Sql = SelectColumns + "WHERE Status = @Status AND IsDeleted = 0 ORDER BY Hostname;";

        await EnsureOpenAsync(cancellationToken);
        await using SqlCommand command = new(Sql, Connection, Transaction);
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.Add("@Status", SqlDbType.VarChar, 20).Value = status;

        List<Agent> agents = [];
        await using SqlDataReader reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            agents.Add(Map(reader));
        }

        return agents;
    }

    public Task<long?> LeaseAsync(
        long buildRunId,
        string operatingSystem,
        string leasedBy,
        CancellationToken cancellationToken = default
    ) => TranslateErrorsAsync(async () =>
    {
        await EnsureOpenAsync(cancellationToken);
        await using SqlCommand command = new("cicd.usp_LeaseAgent", Connection, Transaction);
        command.CommandType = CommandType.StoredProcedure;
        command.CommandTimeout = CommandTimeoutSeconds;
        command.Parameters.Add("@BuildRunId", SqlDbType.BigInt).Value = buildRunId;
        command.Parameters.Add("@OperatingSystem", SqlDbType.NVarChar, 50).Value = operatingSystem;
        command.Parameters.Add("@LeasedBy", SqlDbType.NVarChar, 128).Value = leasedBy;
        SqlParameter agentId = command.Parameters.Add("@AgentId", SqlDbType.BigInt);
        agentId.Direction = ParameterDirection.Output;

        await command.ExecuteNonQueryAsync(cancellationToken);
        return agentId.Value is DBNull ? (long?)null : (long)agentId.Value;
    });

    private static Agent Map(SqlDataReader reader) => new()
    {
        AgentId = reader.GetInt64(0),
        Hostname = reader.GetString(1),
        OperatingSystem = reader.GetString(2),
        Status = reader.GetString(3),
        LastHeartbeatAt = reader.IsDBNull(4) ? null : reader.GetDateTime(4),
        CreatedAt = reader.GetDateTime(5),
        CreatedBy = reader.GetString(6),
        UpdatedAt = reader.IsDBNull(7) ? null : reader.GetDateTime(7),
        UpdatedBy = reader.IsDBNull(8) ? null : reader.GetString(8),
        IsDeleted = reader.GetBoolean(9),
    };
}
