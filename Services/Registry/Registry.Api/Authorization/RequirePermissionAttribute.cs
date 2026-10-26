using Microsoft.AspNetCore.Authorization;

namespace Registry.Api.Authorization;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = false)]
public sealed class RequirePermissionAttribute : AuthorizeAttribute
{
    public RequirePermissionAttribute(string permission)
        : base(permission)
    {
        Permission = permission;
    }

    public string Permission { get; }
}
