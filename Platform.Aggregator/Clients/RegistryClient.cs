using System.Net;
using Platform.Aggregator.Dtos;

namespace Platform.Aggregator.Clients;

public sealed class RegistryClient(HttpClient http)
{
    public async Task<PackageDto?> GetPackageAsync(
        long packageId,
        CancellationToken cancellationToken
    )
    {
        using HttpResponseMessage response = await http.GetAsync(
            new Uri($"/api/packages/{packageId}", UriKind.Relative),
            cancellationToken
        );
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<PackageDto>(cancellationToken);
    }
}
