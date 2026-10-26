using Pulumi;
using Pulumi.Keycloak.OpenId;

namespace KeycloakRealm;

public static class ClientScopeExtensions
{
    private static readonly string[] s_builtInDefaultScopes = ["profile", "email", "roles", "web-origins", "acr", "basic"];

    public static ClientScope WithAudiences(
        this ClientScope scope,
        Output<string> realmId,
        IEnumerable<string> audiences,
        CustomResourceOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(scope);
        ArgumentNullException.ThrowIfNull(audiences);

        foreach (string audience in audiences)
        {
            _ = new AudienceProtocolMapper($"{audience}-audience", new AudienceProtocolMapperArgs
            {
                RealmId = realmId,
                ClientScopeId = scope.Id,
                Name = $"{audience} audience",
                IncludedCustomAudience = audience,
                AddToAccessToken = true,
                AddToIdToken = false,
            }, options);
        }

        return scope;
    }

    public static ClientScope WithPermissionsMapper(
        this ClientScope scope,
        Output<string> realmId,
        CustomResourceOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(scope);

        _ = new UserClientRoleProtocolMapper("permissions-mapper", new UserClientRoleProtocolMapperArgs
        {
            RealmId = realmId,
            ClientScopeId = scope.Id,
            Name = "client roles as permissions",
            ClaimName = "permissions",
            ClaimValueType = "String",
            Multivalued = true,
            AddToAccessToken = true,
            AddToIdToken = false,
            AddToUserinfo = false,
        }, options);

        return scope;
    }

    public static Client WithDefaultScopes(
        this Client client,
        Output<string> realmId,
        ClientScope platformScope,
        CustomResourceOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentNullException.ThrowIfNull(platformScope);

        _ = new ClientDefaultScopes($"{client.GetResourceName()}-default-scopes", new ClientDefaultScopesArgs
        {
            RealmId = realmId,
            ClientId = client.Id,
            DefaultScopes = platformScope.Name.Apply(name => s_builtInDefaultScopes.Append(name).ToList()),
        }, options);

        return client;
    }
}
