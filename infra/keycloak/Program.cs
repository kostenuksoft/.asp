using KeycloakRealm;
using Pulumi;
using Pulumi.Keycloak;
using Pulumi.Keycloak.OpenId;

return await Deployment.RunAsync(() =>
{
    const string RealmName = "DevHub";

    Pulumi.Config config = new();
    string keycloakUrl = config.Get("keycloakUrl") ?? "http://localhost:8080";

    Provider provider = new("keycloak", new ProviderArgs
    {
        Url = keycloakUrl,
        Realm = "master",
        ClientId = "admin-cli",
        Username = "admin",
        Password = config.RequireSecret("adminPassword"),
    });
    CustomResourceOptions options = new() { Provider = provider };

    Realm realm = new("devhub", new RealmArgs
    {
        RealmName = RealmName,
        DisplayName = "DevHub",
        Enabled = true,
        RegistrationAllowed = true,
        ResetPasswordAllowed = true,
        RememberMe = true,
        LoginWithEmailAllowed = true,
        LoginTheme = "keycloak",
        SslRequired = "external",
    }, options);

    List<ResourceServer> resourceServers = PermissionMatrix.ResourceServers
        .Select(spec => new ResourceServer(spec, realm.Id, options))
        .ToList();

    Dictionary<string, Role> realmRoles = PermissionMatrix.Roles.ToDictionary(
        roleName => roleName,
        roleName => new Role($"realm-role-{roleName}", new RoleArgs
        {
            RealmId = realm.Id,
            Name = roleName,
            Description = $"DevHub {roleName}",
            CompositeRoles = resourceServers.SelectMany(server => server.PermissionRoleIdsFor(roleName)).ToList(),
        }, options)
    );

    foreach (ResourceServer server in resourceServers)
    {
        server.GrantByRole(realmRoles);
    }

    ClientScope platformScope = new ClientScope("devhub-apis", new ClientScopeArgs
    {
        RealmId = realm.Id,
        Name = "devhub-apis",
        Description = "Audiences of the DevHub APIs and the permissions claim",
        IncludeInTokenScope = true
    }, options)
        .WithAudiences(realm.Id, PermissionMatrix.Audiences, options)
        .WithPermissionsMapper(realm.Id, options);

    Client postman = KeycloakClientFactory
        .AuthorizationCode("postman", "Postman", realm.Id, ["https://oauth.pstmn.io/v1/callback"], options)
        .WithDefaultScopes(realm.Id, platformScope, options);
    Client swagger = KeycloakClientFactory
        .PublicWithPkce(
            "swagger",
            "Swagger UI",
            realm.Id,
            ["https://localhost:7178/swagger/oauth2-redirect.html", "https://localhost:7179/swagger/oauth2-redirect.html"],
            ["https://localhost:7178", "https://localhost:7179"],
            options
        )
        .WithDefaultScopes(realm.Id, platformScope, options);
    Client aggregator = KeycloakClientFactory
        .ClientCredentials("aggregator", "Platform aggregator", realm.Id, options)
        .WithDefaultScopes(realm.Id, platformScope, options);

    foreach (string permission in PermissionMatrix.AggregatorPermissions)
    {
        ResourceServer owner = resourceServers.Single(server => server.PermissionRoles.ContainsKey(permission));
        _ = new ClientServiceAccountRole($"aggregator-{permission}", new ClientServiceAccountRoleArgs
        {
            RealmId = realm.Id,
            ServiceAccountUserId = aggregator.ServiceAccountUserId,
            ClientId = owner.Client.Id,
            Role = permission,
        }, CustomResourceOptions.Merge(options, new CustomResourceOptions { DependsOn = { owner.PermissionRoles[permission] } }));
    }

    IReadOnlyList<string> users = TestUsers.Create(realm.Id, realmRoles, config.RequireSecret("testUserPassword"), options);

    return new Dictionary<string, object?>
    {
        ["RealmUrl"] = $"{keycloakUrl}/realms/{RealmName}",
        ["AdminConsoleUrl"] = $"{keycloakUrl}/admin/master/console/#/{RealmName}",
        ["ClientIds"] = PermissionMatrix.ResourceServers.Select(spec => spec.ClientId).Prepend("aggregator").Prepend("swagger").Prepend("postman").ToList(),
        ["ClientSecrets"] = Output.CreateSecret(Output.Tuple(postman.ClientSecret, aggregator.ClientSecret).Apply(secrets =>
            new Dictionary<string, string>
            {
                ["postman"] = secrets.Item1,
                ["aggregator"] = secrets.Item2,
            })),
        ["Users"] = users,
        ["SwaggerClientId"] = swagger.ClientId,
    };
});
