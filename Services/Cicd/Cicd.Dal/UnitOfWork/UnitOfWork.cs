using System.Data;
using Cicd.Dal.Repositories;
using Cicd.Dal.Repositories.Interfaces;
using Microsoft.Data.SqlClient;

namespace Cicd.Dal.UnitOfWork;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly SqlConnection _connection;
    private SqlTransaction? _transaction;

    public UnitOfWork(string connectionString)
    {
        _connection = new SqlConnection(connectionString);
        Pipelines = new PipelineRepository(_connection, () => _transaction);
        BuildRuns = new BuildRunRepository(_connection, () => _transaction);
        Artifacts = new ArtifactRepository(_connection, () => _transaction);
        Agents = new AgentRepository(_connection, () => _transaction);
        Packages = new PackageRepository(_connection, () => _transaction);
        ProcessedEvents = new ProcessedEventRepository(_connection, () => _transaction);
    }

    public IPipelineRepository Pipelines { get; }

    public IBuildRunRepository BuildRuns { get; }

    public IArtifactRepository Artifacts { get; }

    public IAgentRepository Agents { get; }

    public IPackageRepository Packages { get; }

    public IProcessedEventRepository ProcessedEvents { get; }

    public async Task BeginTransactionAsync(
        IsolationLevel isolationLevel = IsolationLevel.ReadCommitted,
        CancellationToken cancellationToken = default
    )
    {
        if (_transaction is not null)
        {
            throw new InvalidOperationException("A transaction is already active in this unit of work.");
        }

        if (_connection.State != ConnectionState.Open)
        {
            await _connection.OpenAsync(cancellationToken);
        }

        _transaction = (SqlTransaction)await _connection.BeginTransactionAsync(isolationLevel, cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            throw new InvalidOperationException("There is no active transaction to commit.");
        }

        try
        {
            await _transaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_transaction is null)
        {
            return;
        }

        try
        {
            if (_transaction.Connection is not null)
            {
                await _transaction.RollbackAsync(cancellationToken);
            }
        }
        finally
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_transaction is not null)
        {
            await _transaction.DisposeAsync();
            _transaction = null;
        }

        await _connection.DisposeAsync();
    }
}
