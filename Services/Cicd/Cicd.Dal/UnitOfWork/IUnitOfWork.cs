using System.Data;
using Cicd.Dal.Repositories.Interfaces;

namespace Cicd.Dal.UnitOfWork;

public interface IUnitOfWork : IAsyncDisposable
{
    IPipelineRepository Pipelines { get; }

    IBuildRunRepository BuildRuns { get; }

    IArtifactRepository Artifacts { get; }

    IAgentRepository Agents { get; }

    IPackageRepository Packages { get; }

    IProcessedEventRepository ProcessedEvents { get; }

    Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default
    );

    Task CommitAsync(CancellationToken cancellationToken = default);

    Task RollbackAsync(CancellationToken cancellationToken = default);
}
