namespace Registry.Api.Authorization;

public static class Permissions
{
    public const string Read = "registry:read";
    public const string Create = "registry:create";
    public const string Update = "registry:update";
    public const string Delete = "registry:delete";
    public const string Publish = "registry:publish";

    public const string ReadScope = "registry.read";
    public const string WriteScope = "registry.write";

    public static IReadOnlyList<string> All { get; } = [Read, Create, Update, Delete, Publish];

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ByScope { get; } =
        new Dictionary<string, IReadOnlyList<string>>
        {
            [ReadScope] = [Read],
            [WriteScope] = [Create, Update, Delete, Publish],
        };

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ByAuthServiceRole { get; } =
        new Dictionary<string, IReadOnlyList<string>>
        {
            ["Admin"] = All,
            ["User"] = [Read],
        };
}
