namespace Auth.Api.Data;

public sealed class RefreshToken
{
    public long RefreshTokenId { get; private set; }

    public required string UserId { get; init; }

    public required string TokenHash { get; init; }

    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;

    public required DateTime ExpiresAt { get; init; }

    public DateTime? RevokedAt { get; private set; }

    public string? RevokedReason { get; private set; }

    public string? ReplacedByTokenHash { get; private set; }

    public bool IsActive => RevokedAt is null && ExpiresAt > DateTime.UtcNow;

    public void Revoke(
        string reason,
        string? replacedByTokenHash = null
    )
    {
        if (RevokedAt is not null)
        {
            return;
        }

        RevokedAt = DateTime.UtcNow;
        RevokedReason = reason;
        ReplacedByTokenHash = replacedByTokenHash;
    }
}
