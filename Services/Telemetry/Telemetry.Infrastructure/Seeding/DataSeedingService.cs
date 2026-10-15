using Microsoft.Extensions.Hosting;

namespace Telemetry.Infrastructure.Seeding;

public sealed class DataSeedingService(
    IEnumerable<IDataSeeder> seeders,
    IHostEnvironment environment
)
    : IHostedService
{
    public async Task StartAsync(CancellationToken cancellationToken)
    {
        if (environment.IsProduction())
        {
            return;
        }

        foreach (IDataSeeder seeder in seeders)
        {
            await seeder.SeedAsync(cancellationToken);
        }
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}
