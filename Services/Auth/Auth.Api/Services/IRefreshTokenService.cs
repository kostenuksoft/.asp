using Auth.Api.Data;

namespace Auth.Api.Services;

public interface IRefreshTokenService
{
    Task<string> IssueAsync(
        ApplicationUser user,
        CancellationToken cancellationToken
    );

    Task<RefreshTokenRotation?> RotateAsync(
        string token,
        CancellationToken cancellationToken
    );

    Task<RefreshToken?> FindActiveAsync(
        string token,
        CancellationToken cancellationToken
    );

    Task RevokeAsync(
        RefreshToken refreshToken,
        string reason,
        CancellationToken cancellationToken
    );

    Task<int> RevokeAllAsync(
        string userId,
        string reason,
        CancellationToken cancellationToken
    );

    Task<IReadOnlyList<RefreshToken>> GetHistoryAsync(
        string userId,
        CancellationToken cancellationToken
    );
}
