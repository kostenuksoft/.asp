using System.Reflection;
using FluentValidation;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Telemetry.Application.Common.Behaviors;

namespace Telemetry.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        Assembly assembly = typeof(DependencyInjection).Assembly;

        services.AddMediatR(options =>
        {
            options.LicenseKey = configuration["LuckyPenny:LicenseKey"];
            options.RegisterServicesFromAssembly(assembly);
            options.AddOpenBehavior(typeof(LoggingBehavior<,>));
            options.AddOpenBehavior(typeof(ValidationBehavior<,>));
            options.AddOpenBehavior(typeof(PerformanceBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);
        return services;
    }
}
