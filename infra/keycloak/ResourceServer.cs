using Pulumi;
using Pulumi.Keycloak;
using Pulumi.Keycloak.OpenId;
using Pulumi.Keycloak.OpenId.Inputs;

namespace KeycloakRealm;

public sealed class ResourceServer
{
    private readonly ResourceServerSpec _spec;
    private readonly Output<string> _realmId;
    private readonly CustomResourceOptions _options;
    private readonly Dictionary<string, ClientAuthorizationScope> _authorizationScopes = [];

    public ResourceServer(
        ResourceServerSpec spec,
        Output<string> realmId,
        CustomResourceOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(spec);

        _spec = spec;
        _realmId = realmId;
        _options = options;

        Client = KeycloakClientFactory.ResourceServer(spec.ClientId, spec.DisplayName, realmId, options);

        foreach (string scope in spec.Scopes)
        {
            PermissionRoles[scope] = new Role($"{spec.ClientId}-role-{scope}", new RoleArgs
            {
                RealmId = realmId,
                ClientId = Client.Id,
                Name = scope,
                Description = $"Permission {scope} on {spec.ClientId}",
            }, options);

            string resourceName = $"{spec.ClientId}-scope-{scope}";
            _authorizationScopes[scope] = new ClientAuthorizationScope(resourceName, new ClientAuthorizationScopeArgs
            {
                RealmId = realmId,
                ResourceServerId = Client.ResourceServerId,
                Name = scope,
            }, options);
        }

        CustomResourceOptions afterScopes = CustomResourceOptions.Merge(
            options,
            new CustomResourceOptions { DependsOn = _authorizationScopes.Values.ToList<Resource>() }
        );
        Resource = new ClientAuthorizationResource($"{spec.ClientId}-resource", new ClientAuthorizationResourceArgs
        {
            RealmId = realmId,
            ResourceServerId = Client.ResourceServerId,
            Name = spec.ClientId,
            DisplayName = spec.DisplayName,
            Scopes = spec.Scopes.ToList(),
        }, afterScopes);
    }

    public Client Client { get; }

    public ClientAuthorizationResource Resource { get; }

    public Dictionary<string, Role> PermissionRoles { get; } = [];

    public IEnumerable<Output<string>> PermissionRoleIdsFor(string realmRole) =>
        _spec.ScopesByRole.TryGetValue(realmRole, out IReadOnlyList<string>? scopes)
            ? scopes.Select(scope => PermissionRoles[scope].Id)
            : [];

    public void GrantByRole(IReadOnlyDictionary<string, Role> realmRoles)
    {
        ArgumentNullException.ThrowIfNull(realmRoles);

        Dictionary<string, ClientRolePolicy> policies = [];
        foreach (string roleName in _spec.ScopesByRole.Keys)
        {
            policies[roleName] = new ClientRolePolicy($"{_spec.ClientId}-policy-{roleName}", new ClientRolePolicyArgs
            {
                RealmId = _realmId,
                ResourceServerId = Client.ResourceServerId,
                Name = $"role {roleName}",
                Type = "role",
                Logic = "POSITIVE",
                DecisionStrategy = "UNANIMOUS",
                Roles =
                {
                    new ClientRolePolicyRoleArgs
                    {
                        Id = realmRoles[roleName].Id,
                        Required = true,
                    },
                },
            }, _options);
        }

        foreach (string scope in _spec.Scopes)
        {
            List<string> grantedTo = _spec.ScopesByRole
                .Where(entry => entry.Value.Contains(scope))
                .Select(entry => entry.Key)
                .ToList();

            string resourceName = $"{_spec.ClientId}-permission-{scope}";
            _ = new ClientAuthorizationPermission(resourceName, new ClientAuthorizationPermissionArgs
            {
                RealmId = _realmId,
                ResourceServerId = Client.ResourceServerId,
                Name = $"{scope} permission",
                Type = "scope",
                DecisionStrategy = "AFFIRMATIVE",
                Resources = { Resource.Id },
                Scopes = { _authorizationScopes[scope].Id },
                Policies = grantedTo.Select(roleName => policies[roleName].Id).ToList(),
            }, _options);
        }
    }
}
