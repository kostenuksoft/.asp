namespace KeycloakRealm;

public static class PermissionMatrix
{
    public const string Admin = "admin";
    public const string Manager = "manager";
    public const string User = "user";
    public const string Guest = "guest";

    public static IReadOnlyList<string> Roles { get; } = [Admin, Manager, User, Guest];

    public static IReadOnlyList<ResourceServerSpec> ResourceServers { get; } =
    [
        new(
            "cicd-api",
            "CI/CD runs",
            ["cicd:read", "cicd:create", "cicd:update", "cicd:delete", "cicd:approve"],
            new Dictionary<string, IReadOnlyList<string>>
            {
                [Admin] = ["cicd:read", "cicd:create", "cicd:update", "cicd:delete", "cicd:approve"],
                [Manager] = ["cicd:read", "cicd:update", "cicd:approve"],
                [User] = ["cicd:read", "cicd:create"],
            }
        ),
        new(
            "registry-api",
            "Package registry",
            ["registry:read", "registry:create", "registry:update", "registry:delete", "registry:publish"],
            new Dictionary<string, IReadOnlyList<string>>
            {
                [Admin] =
                    ["registry:read", "registry:create", "registry:update", "registry:delete", "registry:publish"],
                [Manager] = ["registry:read", "registry:update", "registry:publish"],
                [User] = ["registry:read"],
                [Guest] = ["registry:read"],
            }
        ),
        new(
            "secrets-api",
            "Pipeline secrets",
            ["secrets:read", "secrets:rotate", "secrets:revoke"],
            new Dictionary<string, IReadOnlyList<string>>
            {
                [Admin] = ["secrets:read", "secrets:rotate", "secrets:revoke"],
            }
        ),
    ];

    public static IReadOnlyList<string> Audiences { get; } =
        ["cicd-api", "registry-api", "telemetry-api", "secrets-api"];

    public static IReadOnlyList<string> AggregatorPermissions { get; } = ["cicd:read", "registry:read"];
}
