using Platform.Aggregator.Dtos;

namespace Platform.Aggregator.Clients;

public sealed class TelemetryClient(HttpClient http)
{
    public Task<PagedResultDto<DiscussionDto>?> GetDiscussionsAsync(
        string packageKey,
        int pageSize,
        CancellationToken cancellationToken
    ) => http.GetFromJsonAsync<PagedResultDto<DiscussionDto>>(
        new Uri($"/api/discussions?packageKey={Uri.EscapeDataString(packageKey)}&pageSize={pageSize}", UriKind.Relative),
        cancellationToken
    );

    public Task<PagedResultDto<CrashReportDto>?> GetCrashReportsAsync(
        string packageKey,
        int pageSize,
        CancellationToken cancellationToken
    ) => http.GetFromJsonAsync<PagedResultDto<CrashReportDto>>(
        new Uri($"/api/crash-reports?packageKey={Uri.EscapeDataString(packageKey)}&pageSize={pageSize}", UriKind.Relative),
        cancellationToken
    );
}
