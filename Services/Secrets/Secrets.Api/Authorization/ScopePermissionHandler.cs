using Microsoft.AspNetCore.Authorization;

namespace Secrets.Api.Authorization;

public sealed class ScopePermissionHandler : AuthorizationHandler<PermissionRequirement>
{
    private const string ScopeClaim = "scope";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        bool granted = context.User.FindAll(ScopeClaim)
            .SelectMany(claim => claim.Value.Split(' ', StringSplitOptions.RemoveEmptyEntries))
            .Any(scope => Permissions.ByScope.TryGetValue(scope, out IReadOnlyList<string>? permissions)
                && permissions.Contains(requirement.Permission));

        if (granted)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }
}
