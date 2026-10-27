using Duende.IdentityServer.EntityFramework.DbContexts;
using Duende.IdentityServer.EntityFramework.Mappers;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Platform.IdentityServer.Data;

public static class IdentityServerSeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        await services.GetRequiredService<PersistedGrantDbContext>().Database.MigrateAsync();
        await services.GetRequiredService<ApplicationDbContext>().Database.MigrateAsync();

        ConfigurationDbContext configurationContext = services.GetRequiredService<ConfigurationDbContext>();
        await configurationContext.Database.MigrateAsync();
        await SeedConfigurationAsync(configurationContext, configuration);

        await SeedUserAsync(services.GetRequiredService<UserManager<ApplicationUser>>(), configuration);
    }

    private static async Task SeedConfigurationAsync(
        ConfigurationDbContext context,
        IConfiguration configuration
    )
    {
        IdentityServerClientSettings settings = configuration
            .GetSection(IdentityServerClientSettings.SectionName)
            .Get<IdentityServerClientSettings>()
            ?? throw new InvalidOperationException($"Section '{IdentityServerClientSettings.SectionName}' is missing.");

        foreach (Duende.IdentityServer.Models.IdentityResource resource in Config.IdentityResources)
        {
            if (!await context.IdentityResources.AnyAsync(row => row.Name == resource.Name))
            {
                context.IdentityResources.Add(resource.ToEntity());
            }
        }

        foreach (Duende.IdentityServer.Models.ApiScope scope in Config.ApiScopes)
        {
            if (!await context.ApiScopes.AnyAsync(row => row.Name == scope.Name))
            {
                context.ApiScopes.Add(scope.ToEntity());
            }
        }

        foreach (Duende.IdentityServer.Models.ApiResource resource in Config.ApiResources)
        {
            if (!await context.ApiResources.AnyAsync(row => row.Name == resource.Name))
            {
                context.ApiResources.Add(resource.ToEntity());
            }
        }

        foreach (Duende.IdentityServer.Models.Client client in Config.Clients(settings))
        {
            if (!await context.Clients.AnyAsync(row => row.ClientId == client.ClientId))
            {
                context.Clients.Add(client.ToEntity());
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task SeedUserAsync(
        UserManager<ApplicationUser> userManager,
        IConfiguration configuration
    )
    {
        string? userName = configuration["IdentityServer:SeedUser:UserName"];
        string? email = configuration["IdentityServer:SeedUser:Email"];
        string? password = configuration["IdentityServer:SeedUser:Password"];
        if (string.IsNullOrWhiteSpace(userName)
            || string.IsNullOrWhiteSpace(email)
            || string.IsNullOrWhiteSpace(password))
        {
            return;
        }

        if (await userManager.FindByNameAsync(userName) is not null)
        {
            return;
        }

        ApplicationUser user = new()
        {
            UserName = userName,
            Email = email,
            EmailConfirmed = true,
            DisplayName = userName,
        };

        IdentityResult created = await userManager.CreateAsync(user, password);
        if (!created.Succeeded)
        {
            string errors = string.Join("; ", created.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Seeding the IdentityServer user failed: {errors}");
        }
    }
}
