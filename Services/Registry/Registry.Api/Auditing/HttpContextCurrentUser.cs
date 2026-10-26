using System.Security.Claims;
using Registry.Dal.Auditing;

namespace Registry.Api.Auditing;

public sealed class HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private static readonly string[] s_userNameClaims = ["unique_name", "preferred_username", "client_id", "sub"];

    public string? UserName
    {
        get
        {
            ClaimsPrincipal? user = httpContextAccessor.HttpContext?.User;
            if (user?.Identity?.IsAuthenticated != true)
            {
                return null;
            }

            return s_userNameClaims
                .Select(claimType => user.FindFirst(claimType)?.Value)
                .FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }
    }
}
