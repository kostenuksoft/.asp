using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Net.Http.Headers;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Platform.ServiceDefaults;

public static class PlatformAuthentication
{
    public const string Scheme = "platform";
    public const string AuthServiceScheme = "auth";
    public const string IdentityServerScheme = "identityserver";
    public const string KeycloakScheme = "keycloak";
    public const string RoleClaimType = "role";

    private const string BearerPrefix = "Bearer ";
    private const string KeycloakRealmSegment = "/realms/";
    private const int MinimumSecretLength = 32;

    public static TokenValidationParameters AuthServiceTokenParameters(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string issuer = Required(configuration, "Jwt:Issuer");
        string audience = Required(configuration, "Jwt:Audience");
        string secretKey = Required(configuration, "Jwt:SecretKey");
        if (secretKey.Length < MinimumSecretLength)
        {
            throw new InvalidOperationException(
                $"Jwt:SecretKey must be at least {MinimumSecretLength} characters (256 bits for HS256)."
            );
        }

        return new TokenValidationParameters
        {
            ValidIssuer = issuer,
            ValidAudience = audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey)),
            NameClaimType = JwtRegisteredClaimNames.UniqueName,
            RoleClaimType = RoleClaimType,
            ClockSkew = TimeSpan.FromSeconds(30),
        };
    }

    public static Func<HttpContext, string?> SchemeByIssuer(IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(configuration);

        string? identityServerIssuer = configuration["IdentityServer:Authority"]?.TrimEnd('/');
        return context =>
        {
            string? issuer = ReadIssuer(context.Request.Headers[HeaderNames.Authorization].ToString());
            if (issuer is null)
            {
                return AuthServiceScheme;
            }

            if (identityServerIssuer is not null
                && string.Equals(issuer.TrimEnd('/'), identityServerIssuer, StringComparison.OrdinalIgnoreCase))
            {
                return IdentityServerScheme;
            }

            return issuer.Contains(KeycloakRealmSegment, StringComparison.Ordinal) ? KeycloakScheme : AuthServiceScheme;
        };
    }

    private static string? ReadIssuer(string authorization)
    {
        if (!authorization.StartsWith(BearerPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        string token = authorization[BearerPrefix.Length..].Trim();
        JsonWebTokenHandler handler = new();
        return handler.CanReadToken(token) ? handler.ReadJsonWebToken(token).Issuer : null;
    }

    private static string Required(
        IConfiguration configuration,
        string key
    ) => configuration[key] is { Length: > 0 } value
        ? value
        : throw new InvalidOperationException($"Configuration value '{key}' is missing.");
}
