using System.Text.Json.Serialization;

namespace Platform.Aggregator.Security;

public sealed record TokenEndpointResponse(
    [property: JsonPropertyName("access_token")] string AccessToken,
    [property: JsonPropertyName("expires_in")] int ExpiresIn
);
