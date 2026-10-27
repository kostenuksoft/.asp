using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Auth.Api.Data;
using Auth.Api.Options;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Platform.ServiceDefaults;

namespace Auth.Api.Services;

public sealed class JwtTokenService(
    IOptions<JwtOptions> options,
    IOptionsMonitor<JwtBearerOptions> bearerOptions
)
    : IJwtTokenService
{
    private readonly JwtOptions _settings = options.Value;

    public AccessToken GenerateAccessToken(
        ApplicationUser user,
        IEnumerable<string> roles
    )
    {
        ArgumentNullException.ThrowIfNull(user);
        ArgumentNullException.ThrowIfNull(roles);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id),
            new(JwtRegisteredClaimNames.UniqueName, user.UserName!),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];
        claims.AddRange(roles.Select(role => new Claim(PlatformAuthentication.RoleClaimType, role)));

        DateTime expiresAt = DateTime.UtcNow.AddMinutes(_settings.AccessTokenMinutes);
        SymmetricSecurityKey key = new(Encoding.UTF8.GetBytes(_settings.SecretKey));
        JwtSecurityToken token = new(
            issuer: _settings.Issuer,
            audience: _settings.Audience,
            claims: claims,
            expires: expiresAt,
            signingCredentials: new SigningCredentials(key, SecurityAlgorithms.HmacSha256)
        );

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAt);
    }

    public string GenerateRefreshToken() => Base64UrlEncoder.Encode(RandomNumberGenerator.GetBytes(64));

    public ClaimsPrincipal? ValidateToken(string token)
    {
        JwtSecurityTokenHandler handler = new() { MapInboundClaims = false };
        try
        {
            TokenValidationParameters parameters = bearerOptions
                .Get(JwtBearerDefaults.AuthenticationScheme)
                .TokenValidationParameters;
            return handler.ValidateToken(token, parameters, out _);
        }
        catch (Exception exception) when (exception is SecurityTokenException or ArgumentException)
        {
            return null;
        }
    }
}
