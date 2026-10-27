using Cicd.Dal.Repositories.Interfaces;
using Cicd.Domain.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Cicd.Dal.Repositories;

public sealed class PipelineRepository(
    SqlConnection connection,
    Func<SqlTransaction?> currentTransaction
)
    : BaseDapperRepository<Pipeline>(connection, currentTransaction, "cicd.Pipeline", "PipelineId"), IPipelineRepository
{
    protected override string InsertSql =>
        "INSERT INTO cicd.Pipeline (Name, RepositoryUrl, DefaultBranch, PackageId, PackageKey, IsEnabled, CreatedBy) " +
        "OUTPUT INSERTED.PipelineId " +
        "VALUES (@Name, @RepositoryUrl, @DefaultBranch, @PackageId, @PackageKey, @IsEnabled, @CreatedBy);";

    public Task<Pipeline?> GetByNameAsync(
        string name,
        CancellationToken cancellationToken = default
    ) => Connection.QuerySingleOrDefaultAsync<Pipeline>(Command(
        "SELECT * FROM cicd.Pipeline WHERE Name = @Name AND IsDeleted = 0;",
        new { Name = name },
        cancellationToken
    ));

    public Task<int> UpdatePackageKeyAsync(
        long packageId,
        string packageKey,
        string updatedBy,
        CancellationToken cancellationToken = default
    ) => TranslateErrorsAsync(() => Connection.ExecuteAsync(Command(
        "UPDATE cicd.Pipeline SET PackageKey = @PackageKey, UpdatedAt = SYSUTCDATETIME(), UpdatedBy = @UpdatedBy " +
        "WHERE PackageId = @PackageId AND IsDeleted = 0 AND PackageKey <> @PackageKey;",
        new { PackageId = packageId, PackageKey = packageKey, UpdatedBy = updatedBy },
        cancellationToken
    )));
}
