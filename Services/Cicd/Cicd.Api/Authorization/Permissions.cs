namespace Cicd.Api.Authorization;

public static class Permissions
{
    public const string Read = "cicd:read";
    public const string Create = "cicd:create";
    public const string Update = "cicd:update";
    public const string Delete = "cicd:delete";
    public const string Approve = "cicd:approve";

    public const string ReadScope = "cicd.read";
    public const string WriteScope = "cicd.write";

    public static IReadOnlyList<string> All { get; } = [Read, Create, Update, Delete, Approve];

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ByScope { get; } = new Dictionary<string, IReadOnlyList<string>>
    {
        [ReadScope] = [Read],
        [WriteScope] = [Create, Update, Delete, Approve],
    };

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ByAuthServiceRole { get; } = new Dictionary<string, IReadOnlyList<string>>
    {
        ["Admin"] = All,
        ["User"] = [Read, Create],
    };
}
