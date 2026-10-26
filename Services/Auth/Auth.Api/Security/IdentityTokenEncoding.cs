using System.Text;
using Microsoft.AspNetCore.WebUtilities;

namespace Auth.Api.Security;

public static class IdentityTokenEncoding
{
    public static string Encode(string token) => WebEncoders.Base64UrlEncode(Encoding.UTF8.GetBytes(token));

    public static string? TryDecode(string encodedToken)
    {
        try
        {
            return Encoding.UTF8.GetString(WebEncoders.Base64UrlDecode(encodedToken));
        }
        catch (FormatException)
        {
            return null;
        }
    }
}
