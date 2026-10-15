using FluentValidation;
using MediatR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MongoDB.Bson;
using Telemetry.Application;
using Telemetry.Application.Discussions.Commands.CreateDiscussion;
using Telemetry.Application.Discussions.Queries.GetDiscussionById;
using Telemetry.Domain.Entities;
using Telemetry.Domain.Enums;
using Telemetry.Domain.Interfaces;
using Xunit;

namespace Telemetry.Tests.Application;

public sealed class ValidationPipelineTests
{
    [Fact]
    public async Task InvalidCommandIsRejectedBeforeTheHandler()
    {
        StubDiscussionRepository repository = new();
        await using ServiceProvider provider = BuildProvider(repository);
        ISender sender = provider.GetRequiredService<ISender>();

        ValidationException exception = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(CreateCommand(packageId: 0), TestContext.Current.CancellationToken)
        );

        Assert.Contains(exception.Errors, failure => failure.PropertyName == nameof(CreateDiscussionCommand.PackageId));
        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task DuplicateTitleIsRejectedByTheAsyncRule()
    {
        StubDiscussionRepository repository = new() { TitleTaken = true };
        await using ServiceProvider provider = BuildProvider(repository);
        ISender sender = provider.GetRequiredService<ISender>();

        ValidationException exception = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(CreateCommand(packageId: 2), TestContext.Current.CancellationToken)
        );

        Assert.Contains(exception.Errors, failure => failure.PropertyName == nameof(CreateDiscussionCommand.Title));
        Assert.Equal(0, repository.AddCalls);
    }

    [Fact]
    public async Task MalformedObjectIdIsRejectedForQueries()
    {
        await using ServiceProvider provider = BuildProvider(new StubDiscussionRepository());
        ISender sender = provider.GetRequiredService<ISender>();

        await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new GetDiscussionByIdQuery("not-an-object-id"), TestContext.Current.CancellationToken)
        );
    }

    [Fact]
    public async Task ValidCommandReachesTheHandler()
    {
        StubDiscussionRepository repository = new();
        await using ServiceProvider provider = BuildProvider(repository);
        ISender sender = provider.GetRequiredService<ISender>();

        string id = await sender.Send(CreateCommand(packageId: 2), TestContext.Current.CancellationToken);

        Assert.True(ObjectId.TryParse(id, out _));
        Assert.Equal(1, repository.AddCalls);
    }

    private static ServiceProvider BuildProvider(StubDiscussionRepository repository)
    {
        ServiceCollection services = new();
        services.AddLogging();
        services.AddApplication(new ConfigurationBuilder().Build());
        services.AddSingleton<IDiscussionRepository>(repository);
        return services.BuildServiceProvider();
    }

    private static CreateDiscussionCommand CreateCommand(long packageId) => new(
        packageId,
        "platform-team/devhub-auth",
        "devhub-auth",
        "platform-team",
        "Token refresh fails behind a proxy",
        DiscussionCategory.Bug,
        "m.shevchenko"
    );

    private sealed class StubDiscussionRepository : IDiscussionRepository
    {
        public bool TitleTaken { get; init; }

        public int AddCalls { get; private set; }

        public Task<Discussion?> GetByIdAsync(
            ObjectId id,
            CancellationToken cancellationToken = default
        ) => Task.FromResult<Discussion?>(null);

        public Task<bool> ExistsAsync(
            ObjectId id,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(false);

        public Task<bool> ExistsAsync(
            string packageKey,
            string title,
            CancellationToken cancellationToken = default
        ) => Task.FromResult(TitleTaken);

        public Task AddAsync(
            Discussion discussion,
            CancellationToken cancellationToken = default
        )
        {
            AddCalls++;
            return Task.CompletedTask;
        }

        public Task UpdateAsync(
            Discussion discussion,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;

        public Task DeleteAsync(
            Discussion discussion,
            CancellationToken cancellationToken = default
        ) => Task.CompletedTask;
    }
}
