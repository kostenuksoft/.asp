using System.Security.Cryptography;
using System.Text;
using Auth.Api.Data;
using Auth.Api.Options;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace Auth.Api.Services;

public sealed class RefreshTokenService(
    ApplicationDbContext context,
    IJwtTokenService tokens,
    IOptions<JwtOptions> options,
    ILogger<RefreshTokenService> logger
)
    : IRefreshTokenService
{
    private const string RotatedReason = "rotated";
    private const string ReuseReason = "reuse detected";

    public async Task<string> IssueAsync(
        ApplicationUser user,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(user);

        string token = tokens.GenerateRefreshToken();
        context.RefreshTokens.Add(NewRow(user.Id, token));
        await context.SaveChangesAsync(cancellationToken);
        return token;
    }

    public async Task<RefreshTokenRotation?> RotateAsync(
        string token,
        CancellationToken cancellationToken
    )
    {
        string hash = Hash(token);
        RefreshToken? current = await context.RefreshTokens.SingleOrDefaultAsync(
            row => row.TokenHash == hash,
            cancellationToken
        );
        if (current is null)
        {
            return null;
        }

        if (current.RevokedAt is not null)
        {
            int revoked = await RevokeAllAsync(current.UserId, ReuseReason, cancellationToken);
            logger.LogWarning(
                "Revoked refresh token presented again for user {UserId}; {Count} active tokens revoked",
                current.UserId,
                revoked
            );
            return null;
        }

        if (!current.IsActive)
        {
            return null;
        }

        ApplicationUser? user = await context.Users.SingleOrDefaultAsync(
            row => row.Id == current.UserId,
            cancellationToken
        );
        if (user is null)
        {
            return null;
        }

        string newToken = tokens.GenerateRefreshToken();
        RefreshToken next = NewRow(user.Id, newToken);
        current.Revoke(RotatedReason, next.TokenHash);
        context.RefreshTokens.Add(next);
        await context.SaveChangesAsync(cancellationToken);

        return new RefreshTokenRotation(user, newToken);
    }

    public async Task<RefreshToken?> FindActiveAsync(
        string token,
        CancellationToken cancellationToken
    )
    {
        string hash = Hash(token);
        RefreshToken? row = await context.RefreshTokens.SingleOrDefaultAsync(
            candidate => candidate.TokenHash == hash,
            cancellationToken
        );
        return row is { IsActive: true } ? row : null;
    }

    public async Task RevokeAsync(
        RefreshToken refreshToken,
        string reason,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(refreshToken);

        refreshToken.Revoke(reason);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<int> RevokeAllAsync(
        string userId,
        string reason,
        CancellationToken cancellationToken
    )
    {
        DateTime now = DateTime.UtcNow;
        List<RefreshToken> active = await context.RefreshTokens
            .Where(row => row.UserId == userId && row.RevokedAt == null && row.ExpiresAt > now)
            .ToListAsync(cancellationToken);

        foreach (RefreshToken row in active)
        {
            row.Revoke(reason);
        }

        await context.SaveChangesAsync(cancellationToken);
        return active.Count;
    }

    public async Task<IReadOnlyList<RefreshToken>> GetHistoryAsync(
        string userId,
        CancellationToken cancellationToken
    ) => await context.RefreshTokens
        .AsNoTracking()
        .Where(row => row.UserId == userId)
        .OrderByDescending(row => row.CreatedAt)
        .ToListAsync(cancellationToken);

    private RefreshToken NewRow(
        string userId,
        string token
    ) => new()
    {
        UserId = userId,
        TokenHash = Hash(token),
        ExpiresAt = DateTime.UtcNow.AddDays(options.Value.RefreshTokenDays),
    };

    private static string Hash(string token) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));
}
