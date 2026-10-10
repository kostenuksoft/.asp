using Cicd.Dal.Repositories.Interfaces;
using Cicd.Domain.Models;
using Dapper;
using Microsoft.Data.SqlClient;

namespace Cicd.Dal.Repositories;

public sealed class ArtifactRepository : BaseDapperRepository<Artifact>, IArtifactRepository
{
    public ArtifactRepository(SqlConnection connection, Func<SqlTransaction?> currentTransaction)
        : base(connection, currentTransaction, "cicd.Artifact", "ArtifactId")
    {
    }

    protected override string InsertSql =>
        "INSERT INTO cicd.Artifact " +
        "(PackageId, PackageKey, PackageName, PublisherSlug, SemVer, SizeBytes, Sha256, StorageUri, RegistrySyncedAt, CreatedBy) " +
        "OUTPUT INSERTED.ArtifactId " +
        "VALUES (@PackageId, @PackageKey, @PackageName, @PublisherSlug, @SemVer, @SizeBytes, @Sha256, @StorageUri, " +
        "@RegistrySyncedAt, @CreatedBy);";

    public Task<Artifact?> GetBySha256Async(string sha256, CancellationToken cancellationToken = default)
        => Connection.QuerySingleOrDefaultAsync<Artifact>(Command(
            "SELECT * FROM cicd.Artifact WHERE Sha256 = @Sha256 AND IsDeleted = 0;",
            new { Sha256 = sha256 },
            cancellationToken));

    public async Task<IReadOnlyList<Artifact>> GetByPackageAsync(long packageId, CancellationToken cancellationToken = default)
    {
        IEnumerable<Artifact> rows = await Connection.QueryAsync<Artifact>(Command(
            "SELECT * FROM cicd.Artifact WHERE PackageId = @PackageId AND IsDeleted = 0 ORDER BY SemVer;",
            new { PackageId = packageId },
            cancellationToken));
        return rows.AsList();
    }

    public Task LinkToRunAsync(ArtifactUsage usage, CancellationToken cancellationToken = default)
        => TranslateErrorsAsync(() => Connection.ExecuteAsync(Command(
            "INSERT INTO cicd.ArtifactUsage (BuildRunId, ArtifactId, UsageKind, CreatedBy) " +
            "VALUES (@BuildRunId, @ArtifactId, @UsageKind, @CreatedBy);",
            usage,
            cancellationToken)));
}
