namespace Auth.Api.Contracts;

public sealed record RefreshTokenDto(
    DateTime CreatedAt,
    DateTime ExpiresAt,
    DateTime? RevokedAt,
    string? RevokedReason,
    bool IsActive
);
