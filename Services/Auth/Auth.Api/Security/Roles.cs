namespace Auth.Api.Security;

public static class Roles
{
    public const string User = "User";
    public const string Admin = "Admin";

    public static IReadOnlyList<string> All { get; } = [User, Admin];
}
