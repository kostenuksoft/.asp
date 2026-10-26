namespace Registry.Api.IntegrationEvents;

public static class RegistryEvents
{
    public const string Exchange = "registry-events";
    public const string PackageCreated = "package.created";
    public const string PackageRenamed = "package.renamed";
    public const string PackageLicensePriceChanged = "package.license_price_changed";
    public const string PackageDeleted = "package.deleted";
}
