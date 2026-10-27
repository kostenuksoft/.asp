using System.Data;
using Cicd.Dal.Repositories.Interfaces;
using Cicd.Domain.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Cicd.Dal.Repositories;

public sealed class PackageRepository(
    SqlConnection connection,
    Func<SqlTransaction?> currentTransaction
)
    : RepositoryBase(connection, currentTransaction), IPackageRepository
{
    private const string UpsertSql =
        "DECLARE @Applied INT; " +
        "UPDATE cicd.Package WITH (UPDLOCK, HOLDLOCK) " +
        "SET PublisherSlug = @PublisherSlug, PackageName = @PackageName, PackageKey = @PackageKey, " +
        "LicensePrice = @LicensePrice, IsDeleted = @IsDeleted, SourceChangedAt = @SourceChangedAt, SyncedAt = SYSUTCDATETIME() " +
        "WHERE PackageId = @PackageId AND (SourceChangedAt IS NULL OR SourceChangedAt < @SourceChangedAt); " +
        "SET @Applied = @@ROWCOUNT; " +
        "IF @Applied = 0 AND NOT EXISTS (SELECT 1 FROM cicd.Package WITH (UPDLOCK, HOLDLOCK) WHERE PackageId = @PackageId) " +
        "BEGIN " +
        "INSERT INTO cicd.Package (PackageId, PublisherSlug, PackageName, PackageKey, LicensePrice, IsDeleted, SourceChangedAt, CreatedBy) " +
        "VALUES (@PackageId, @PublisherSlug, @PackageName, @PackageKey, @LicensePrice, @IsDeleted, @SourceChangedAt, @CreatedBy); " +
        "SET @Applied = @@ROWCOUNT; " +
        "END; " +
        "SELECT @Applied;";

    public Task<Package?> GetByIdAsync(
        long packageId,
        CancellationToken cancellationToken = default
    ) => Connection.QuerySingleOrDefaultAsync<Package>(Command(
        "SELECT * FROM cicd.Package WHERE PackageId = @PackageId;",
        new { PackageId = packageId },
        cancellationToken
    ));

    public async Task<bool> UpsertAsync(
        Package package,
        CancellationToken cancellationToken = default
    )
    {
        ArgumentNullException.ThrowIfNull(package);

        DynamicParameters parameters = new(new
        {
            package.PackageId,
            package.PublisherSlug,
            package.PackageName,
            package.PackageKey,
            package.LicensePrice,
            package.IsDeleted,
            package.CreatedBy,
        });
        parameters.Add("SourceChangedAt", package.SourceChangedAt, DbType.DateTime2);

        int applied = await TranslateErrorsAsync(() =>
            Connection.ExecuteScalarAsync<int>(Command(UpsertSql, parameters, cancellationToken))
        );
        return applied == 1;
    }
}
