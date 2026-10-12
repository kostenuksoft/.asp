using Cicd.Dal.Repositories.Interfaces;
using Cicd.Domain.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Cicd.Dal.Repositories;

public sealed class PipelineRepository : BaseDapperRepository<Pipeline>, IPipelineRepository
{
    public PipelineRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "cicd.Pipeline", "PipelineId")
    {
    }

    protected override string InsertSql =>
        "INSERT INTO cicd.Pipeline (Name, RepositoryUrl, DefaultBranch, PackageId, PackageKey, IsEnabled, CreatedBy) " +
        "OUTPUT INSERTED.PipelineId " +
        "VALUES (@Name, @RepositoryUrl, @DefaultBranch, @PackageId, @PackageKey, @IsEnabled, @CreatedBy);";

    public Task<Pipeline?> GetByNameAsync(string name, CancellationToken cancellationToken = default)
        => Connection.QuerySingleOrDefaultAsync<Pipeline>(Command(
            "SELECT * FROM cicd.Pipeline WHERE Name = @Name AND IsDeleted = 0;",
            new { Name = name },
            cancellationToken));
}
