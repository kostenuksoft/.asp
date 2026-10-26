namespace Auth.Api.Contracts;

public sealed record AuthResponse(
    string AccessToken,
    DateTime ExpiresAt,
    string TokenType
);
