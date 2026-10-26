using Pulumi;
using Pulumi.Keycloak.OpenId;
using Pulumi.Keycloak.OpenId.Inputs;

namespace KeycloakRealm;

public static class KeycloakClientFactory
{
    public static Client AuthorizationCode(
        string clientId,
        string name,
        Output<string> realmId,
        IReadOnlyList<string> redirectUris,
        CustomResourceOptions options
    ) => new(clientId, new ClientArgs
    {
        RealmId = realmId,
        ClientId = clientId,
        Name = name,
        AccessType = "CONFIDENTIAL",
        StandardFlowEnabled = true,
        DirectAccessGrantsEnabled = false,
        PkceCodeChallengeMethod = "S256",
        ValidRedirectUris = redirectUris.ToList(),
    }, options);

    public static Client PublicWithPkce(
        string clientId,
        string name,
        Output<string> realmId,
        IReadOnlyList<string> redirectUris,
        IReadOnlyList<string> webOrigins,
        CustomResourceOptions options
    ) => new(clientId, new ClientArgs
    {
        RealmId = realmId,
        ClientId = clientId,
        Name = name,
        AccessType = "PUBLIC",
        StandardFlowEnabled = true,
        DirectAccessGrantsEnabled = false,
        PkceCodeChallengeMethod = "S256",
        ValidRedirectUris = redirectUris.ToList(),
        WebOrigins = webOrigins.ToList(),
    }, options);

    public static Client ClientCredentials(
        string clientId,
        string name,
        Output<string> realmId,
        CustomResourceOptions options
    ) => new(clientId, new ClientArgs
    {
        RealmId = realmId,
        ClientId = clientId,
        Name = name,
        AccessType = "CONFIDENTIAL",
        StandardFlowEnabled = false,
        DirectAccessGrantsEnabled = false,
        ServiceAccountsEnabled = true,
    }, options);

    public static Client ResourceServer(
        string clientId,
        string name,
        Output<string> realmId,
        CustomResourceOptions options
    ) => new(clientId, new ClientArgs
    {
        RealmId = realmId,
        ClientId = clientId,
        Name = name,
        AccessType = "CONFIDENTIAL",
        StandardFlowEnabled = false,
        DirectAccessGrantsEnabled = false,
        ServiceAccountsEnabled = true,
        Authorization = new ClientAuthorizationArgs
        {
            PolicyEnforcementMode = "ENFORCING",
            DecisionStrategy = "AFFIRMATIVE",
            AllowRemoteResourceManagement = false,
            KeepDefaults = false,
        },
    }, options);
}
