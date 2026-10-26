namespace Secrets.Api.Authorization;

public static class Permissions
{
    public const string Read = "secrets:read";
    public const string Rotate = "secrets:rotate";
    public const string Revoke = "secrets:revoke";

    public const string ReadScope = "secrets.read";
    public const string WriteScope = "secrets.write";

    public static IReadOnlyList<string> All { get; } = [Read, Rotate, Revoke];

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ByScope { get; } = new Dictionary<string, IReadOnlyList<string>>
    {
        [ReadScope] = [Read],
        [WriteScope] = [Rotate, Revoke],
    };

    public static IReadOnlyDictionary<string, IReadOnlyList<string>> ByAuthServiceRole { get; } = new Dictionary<string, IReadOnlyList<string>>
    {
        ["Admin"] = All,
    };
}
