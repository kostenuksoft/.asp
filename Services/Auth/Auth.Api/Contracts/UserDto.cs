using Auth.Api.Data;

namespace Auth.Api.Contracts;

public sealed record UserDto(
    string Id,
    string UserName,
    string Email,
    bool EmailConfirmed,
    string? DisplayName,
    DateTime CreatedAt,
    DateTimeOffset? LockoutEnd
)
{
    public static UserDto From(ApplicationUser user)
    {
        ArgumentNullException.ThrowIfNull(user);

        return new UserDto(
            user.Id,
            user.UserName!,
            user.Email!,
            user.EmailConfirmed,
            user.DisplayName,
            user.CreatedAt,
            user.LockoutEnd
        );
    }
}
