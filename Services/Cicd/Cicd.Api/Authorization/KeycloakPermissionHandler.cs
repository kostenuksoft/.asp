using System.Text.Json;
using Microsoft.AspNetCore.Authorization;

namespace Cicd.Api.Authorization;

public sealed class KeycloakPermissionHandler(ILogger<KeycloakPermissionHandler> logger)
    : AuthorizationHandler<PermissionRequirement>
{
    private const string PermissionsClaim = "permissions";
    private const string AuthorizationClaim = "authorization";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        PermissionRequirement requirement
    )
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentNullException.ThrowIfNull(requirement);

        bool granted = context.User.FindAll(PermissionsClaim).Any(claim => claim.Value == requirement.Permission)
            || IsGrantedByRpt(context.User.FindFirst(AuthorizationClaim)?.Value, requirement.Permission);

        if (granted)
        {
            context.Succeed(requirement);
        }

        return Task.CompletedTask;
    }

    private bool IsGrantedByRpt(
        string? authorizationClaim,
        string permission
    )
    {
        if (authorizationClaim is null)
        {
            return false;
        }

        try
        {
            using JsonDocument document = JsonDocument.Parse(authorizationClaim);
            return document.RootElement.TryGetProperty("permissions", out JsonElement permissions)
                && permissions.EnumerateArray()
                    .Where(entry => entry.TryGetProperty("scopes", out _))
                    .SelectMany(entry => entry.GetProperty("scopes").EnumerateArray())
                    .Any(scope => scope.GetString() == permission);
        }
        catch (JsonException exception)
        {
            logger.LogWarning(
                exception,
                "Malformed {Claim} claim; permission {Permission} not granted",
                AuthorizationClaim,
                permission
            );
            return false;
        }
    }
}
