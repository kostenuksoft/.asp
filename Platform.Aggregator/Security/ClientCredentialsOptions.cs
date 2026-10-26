namespace Platform.Aggregator.Security;

public sealed class ClientCredentialsOptions
{
    public const string SectionName = "IdentityServer";

    public string Authority { get; set; } = string.Empty;

    public string ClientId { get; set; } = string.Empty;

    public string ClientSecret { get; set; } = string.Empty;

    public string Scope { get; set; } = string.Empty;
}
