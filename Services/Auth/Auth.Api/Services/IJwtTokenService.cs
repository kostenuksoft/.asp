using System.Security.Claims;
using Auth.Api.Data;

namespace Auth.Api.Services;

public interface IJwtTokenService
{
    AccessToken GenerateAccessToken(
        ApplicationUser user,
        IEnumerable<string> roles
    );

    string GenerateRefreshToken();

    ClaimsPrincipal? ValidateToken(string token);
}
