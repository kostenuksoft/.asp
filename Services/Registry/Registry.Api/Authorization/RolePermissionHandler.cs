using Microsoft.AspNetCore.Authorization;
using Platform.ServiceDefaults;

namespace Registry.Api.Authorization;

public sealed class RolePermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        bool granted = context.User.FindAll(PlatformAuthentication.RoleClaimType)
            .Any(role => Permissions.ByAuthServiceRole.TryGetValue(role.Value, out IReadOnlyList<string>? permissions)
                && permissions.Contains(requirement.Permission));

        if (granted)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
