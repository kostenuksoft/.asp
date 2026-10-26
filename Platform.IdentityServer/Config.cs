using Duende.IdentityServer;
using Duende.IdentityServer.Models;

namespace Platform.IdentityServer;

public static class Config
{
    public const string CicdRead = "cicd.read";
    public const string CicdWrite = "cicd.write";
    public const string RegistryRead = "registry.read";
    public const string RegistryWrite = "registry.write";
    public const string TelemetryRead = "telemetry.read";
    public const string SecretsRead = "secrets.read";
    public const string SecretsWrite = "secrets.write";

    public static IEnumerable<IdentityResource> IdentityResources { get; } =
    [
        new IdentityResources.OpenId(),
        new IdentityResources.Profile(),
    ];

    public static IEnumerable<ApiScope> ApiScopes { get; } =
    [
        new(CicdRead, "Read pipelines, build runs and agents"),
        new(CicdWrite, "Queue, start, change and delete build runs and pipelines"),
        new(RegistryRead, "Read packages and tags"),
        new(RegistryWrite, "Publish, change, list and delete packages"),
        new(TelemetryRead, "Read discussions and crash reports"),
        new(SecretsRead, "List pipeline secrets"),
        new(SecretsWrite, "Rotate and revoke pipeline secrets"),
    ];

    public static IEnumerable<ApiResource> ApiResources { get; } =
    [
        new("cicd_api", "CI/CD runs") { Scopes = { CicdRead, CicdWrite } },
        new("registry_api", "Package registry") { Scopes = { RegistryRead, RegistryWrite } },
        new("telemetry_api", "Telemetry and discussions") { Scopes = { TelemetryRead } },
        new("secrets_api", "Pipeline secrets") { Scopes = { SecretsRead, SecretsWrite } },
    ];

    public static IEnumerable<Client> Clients(IdentityServerClientSettings settings)
    {
        ArgumentNullException.ThrowIfNull(settings);

        return
        [
            new Client
            {
                ClientId = "postman",
                ClientName = "Postman",
                AllowedGrantTypes = GrantTypes.Code,
                ClientSecrets = { new Secret(settings.PostmanSecret.Sha256()) },
                RedirectUris = { "https://oauth.pstmn.io/v1/callback" },
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    CicdRead,
                    CicdWrite,
                    RegistryRead,
                    RegistryWrite,
                    TelemetryRead,
                    SecretsRead,
                    SecretsWrite,
                },
                AllowOfflineAccess = true,
            },
            new Client
            {
                ClientId = "aggregator",
                ClientName = "Platform aggregator",
                AllowedGrantTypes = GrantTypes.ClientCredentials,
                ClientSecrets = { new Secret(settings.AggregatorSecret.Sha256()) },
                AllowedScopes = { CicdRead, RegistryRead, TelemetryRead },
            },
            new Client
            {
                ClientId = "swagger",
                ClientName = "Swagger UI",
                AllowedGrantTypes = GrantTypes.Code,
                RequirePkce = true,
                RequireClientSecret = false,
                RedirectUris = settings.SwaggerRedirectUris,
                AllowedCorsOrigins = settings.SwaggerOrigins,
                AllowedScopes =
                {
                    IdentityServerConstants.StandardScopes.OpenId,
                    IdentityServerConstants.StandardScopes.Profile,
                    CicdRead,
                    CicdWrite,
                    RegistryRead,
                    RegistryWrite,
                },
            },
        ];
    }
}
