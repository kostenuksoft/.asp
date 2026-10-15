using System.Net;
using Platform.Aggregator.Dtos;

namespace Platform.Aggregator.Clients;

public sealed class CicdClient(HttpClient http)
{
    public async Task<RunTimelineDto?> GetTimelineAsync(
        long buildRunId,
        CancellationToken cancellationToken
    )
    {
        using HttpResponseMessage response = await http.GetAsync(
            new Uri($"/api/build-runs/{buildRunId}/timeline", UriKind.Relative),
            cancellationToken
        );
        if (response.StatusCode == HttpStatusCode.NotFound)
        {
            return null;
        }

        response.EnsureSuccessStatusCode();
        return await response.Content.ReadFromJsonAsync<RunTimelineDto>(cancellationToken);
    }
}
