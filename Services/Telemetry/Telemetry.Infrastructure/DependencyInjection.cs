using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telemetry.Application.Common.Interfaces;
using Telemetry.Domain.Interfaces;
using Telemetry.Infrastructure.Persistence;
using Telemetry.Infrastructure.Seeding;

namespace Telemetry.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(configuration);

        MongoMappings.Register();

        services.Configure<MongoDbOptions>(configuration.GetSection(MongoDbOptions.SectionName));
        services.AddSingleton<MongoDbContext>();

        services.AddScoped<IDiscussionRepository, DiscussionRepository>();
        services.AddScoped<ICrashReportRepository, CrashReportRepository>();
        services.AddScoped<IDiscussionQueries, DiscussionQueries>();
        services.AddScoped<ICrashReportQueries, CrashReportQueries>();

        services.AddSingleton<IDataSeeder, DiscussionSeeder>();
        services.AddSingleton<IDataSeeder, CrashReportSeeder>();
        services.AddHostedService<IndexCreationService>();
        services.AddHostedService<DataSeedingService>();

        return services;
    }
}
