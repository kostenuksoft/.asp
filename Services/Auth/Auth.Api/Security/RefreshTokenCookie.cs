namespace Auth.Api.Security;

public static class RefreshTokenCookie
{
    public const string Name = "devhub_refresh_token";
    private const string Path = "/auth";

    public static string? Read(HttpRequest request)
    {
        ArgumentNullException.ThrowIfNull(request);

        return request.Cookies.TryGetValue(Name, out string? token) && !string.IsNullOrWhiteSpace(token) ? token : null;
    }

    public static void Write(
        HttpResponse response,
        string token,
        int lifetimeDays
    )
    {
        ArgumentNullException.ThrowIfNull(response);

        response.Cookies.Append(Name, token, Options(DateTimeOffset.UtcNow.AddDays(lifetimeDays)));
    }

    public static void Delete(HttpResponse response)
    {
        ArgumentNullException.ThrowIfNull(response);

        response.Cookies.Delete(Name, Options(expires: null));
    }

    private static CookieOptions Options(DateTimeOffset? expires) => new()
    {
        HttpOnly = true,
        Secure = true,
        SameSite = SameSiteMode.Strict,
        Path = Path,
        Expires = expires,
        IsEssential = true,
    };
}
