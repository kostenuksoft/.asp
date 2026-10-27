using System.Net.Http.Json;
using Microsoft.Extensions.Options;

namespace Platform.Aggregator.Security;

public sealed class ClientCredentialsTokenProvider(
    IHttpClientFactory httpClientFactory,
    IOptions<ClientCredentialsOptions> options,
    TimeProvider timeProvider,
    ILogger<ClientCredentialsTokenProvider> logger
)
    : IDisposable
{
    public const string HttpClientName = "identityserver-token";

    private static readonly TimeSpan s_renewBeforeExpiry = TimeSpan.FromSeconds(60);

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _accessToken;
    private DateTimeOffset _renewAt;

    public async Task<string> GetAccessTokenAsync(CancellationToken cancellationToken)
    {
        if (_accessToken is not null && timeProvider.GetUtcNow() < _renewAt)
        {
            return _accessToken;
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_accessToken is not null && timeProvider.GetUtcNow() < _renewAt)
            {
                return _accessToken;
            }

            ClientCredentialsOptions settings = options.Value;
            using FormUrlEncodedContent form = new(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = settings.ClientId,
                ["client_secret"] = settings.ClientSecret,
                ["scope"] = settings.Scope,
            });

            HttpClient client = httpClientFactory.CreateClient(HttpClientName);
            using HttpResponseMessage response = await client.PostAsync(
                new Uri($"{settings.Authority.TrimEnd('/')}/connect/token"),
                form,
                cancellationToken
            );
            response.EnsureSuccessStatusCode();

            TokenEndpointResponse token = await response.Content
                .ReadFromJsonAsync<TokenEndpointResponse>(cancellationToken)
                ?? throw new InvalidOperationException("The token endpoint returned an empty body.");

            _accessToken = token.AccessToken;
            _renewAt = timeProvider.GetUtcNow().AddSeconds(token.ExpiresIn) - s_renewBeforeExpiry;
            logger.LogInformation(
                "Client credentials token for {ClientId} issued, scopes {Scope}, valid {ExpiresIn} s",
                settings.ClientId,
                settings.Scope,
                token.ExpiresIn
            );
            return _accessToken;
        }
        finally
        {
            _gate.Release();
        }
    }

    public void Dispose() => _gate.Dispose();
}
