namespace Auth.Api.Services;

public sealed record AccessToken(
    string Token,
    DateTime ExpiresAt
);
