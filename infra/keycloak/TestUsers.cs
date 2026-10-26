using Pulumi;
using Pulumi.Keycloak;
using Pulumi.Keycloak.Inputs;

namespace KeycloakRealm;

public static class TestUsers
{
    public static IReadOnlyList<string> Create(
        Output<string> realmId,
        IReadOnlyDictionary<string, Role> realmRoles,
        Output<string> temporaryPassword,
        CustomResourceOptions options
    )
    {
        ArgumentNullException.ThrowIfNull(realmRoles);

        List<string> userNames = [];
        foreach ((string roleName, Role role) in realmRoles)
        {
            string userName = $"devhub-{roleName}";
            User user = new(userName, new UserArgs
            {
                RealmId = realmId,
                Username = userName,
                Enabled = true,
                Email = $"{userName}@devhub.local",
                EmailVerified = true,
                FirstName = "DevHub",
                LastName = roleName,
                InitialPassword = new UserInitialPasswordArgs
                {
                    Value = temporaryPassword,
                    Temporary = true,
                },
            }, options);

            _ = new UserRoles($"{userName}-roles", new UserRolesArgs
            {
                RealmId = realmId,
                UserId = user.Id,
                RoleIds = { role.Id },
                Exhaustive = false,
            }, options);

            userNames.Add(userName);
        }

        return userNames;
    }
}
