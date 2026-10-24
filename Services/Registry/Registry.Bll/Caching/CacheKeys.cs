namespace Registry.Bll.Caching;

public static class CacheKeys
{
    public const string Tags = "registry:tags";
    public const string PackageListVersion = "registry:packages:list-version";

    public static string Package(long packageId) => $"registry:package:{packageId}";

    public static string TopPackages(
        int take,
        string listVersion
    ) => $"registry:packages:top:{take}:v{listVersion}";
}
