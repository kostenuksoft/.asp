using Microsoft.AspNetCore.Authorization;

namespace Registry.Api.Authorization;

public sealed class PermissionRequirement(string permission) : IAuthorizationRequirement
{
    public string Permission { get; } = permission;
}
