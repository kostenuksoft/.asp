using Auth.Api.Data;

namespace Auth.Api.Services;

public sealed record RefreshTokenRotation(
    ApplicationUser User,
    string NewToken
);
