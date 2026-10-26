namespace Platform.IdentityServer;

public sealed class IdentityServerClientSettings
{
    public const string SectionName = "IdentityServer:Clients";

    public string PostmanSecret { get; set; } = string.Empty;

    public string AggregatorSecret { get; set; } = string.Empty;

    public ICollection<string> SwaggerRedirectUris { get; } = [];

    public ICollection<string> SwaggerOrigins { get; } = [];
}
