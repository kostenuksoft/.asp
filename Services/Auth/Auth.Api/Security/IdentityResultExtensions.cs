using Microsoft.AspNetCore.Identity;

namespace Auth.Api.Security;

public static class IdentityResultExtensions
{
    public static Dictionary<string, string[]> ToValidationErrors(this IdentityResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Errors
            .GroupBy(error => error.Code)
            .ToDictionary(group => group.Key, group => group.Select(error => error.Description).ToArray());
    }
}
