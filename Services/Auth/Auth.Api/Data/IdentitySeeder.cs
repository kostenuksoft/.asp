using Auth.Api.Security;
using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Data;

public static class IdentitySeeder
{
    public static async Task SeedAsync(
        IServiceProvider services,
        IConfiguration configuration
    )
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        RoleManager<IdentityRole> roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (string role in Roles.All)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                await roleManager.CreateAsync(new IdentityRole(role));
            }
        }

        string? adminUserName = configuration["Auth:AdminUserName"];
        string? adminEmail = configuration["Auth:AdminEmail"];
        string? adminPassword = configuration["Auth:AdminPassword"];
        if (string.IsNullOrWhiteSpace(adminUserName)
            || string.IsNullOrWhiteSpace(adminEmail)
            || string.IsNullOrWhiteSpace(adminPassword))
        {
            return;
        }

        UserManager<ApplicationUser> userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        if (await userManager.FindByNameAsync(adminUserName) is not null)
        {
            return;
        }

        ApplicationUser admin = new()
        {
            UserName = adminUserName,
            Email = adminEmail,
            EmailConfirmed = true,
            DisplayName = "Platform administrator",
        };

        IdentityResult created = await userManager.CreateAsync(admin, adminPassword);
        if (!created.Succeeded)
        {
            string errors = string.Join("; ", created.Errors.Select(error => error.Description));
            throw new InvalidOperationException($"Seeding the administrator failed: {errors}");
        }

        await userManager.AddToRolesAsync(admin, Roles.All);
    }
}
