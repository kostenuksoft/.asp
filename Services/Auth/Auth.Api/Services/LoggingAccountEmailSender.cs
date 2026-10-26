using Auth.Api.Data;

namespace Auth.Api.Services;

public sealed class LoggingAccountEmailSender(ILogger<LoggingAccountEmailSender> logger) : IAccountEmailSender
{
    public Task SendEmailConfirmationAsync(
        ApplicationUser user,
        string encodedToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(user);

        logger.LogInformation(
            "Mail to {Email}: confirm the address with POST /auth/verify-email, userId {UserId}, token {Token}",
            user.Email,
            user.Id,
            encodedToken
        );
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(
        ApplicationUser user,
        string encodedToken,
        CancellationToken cancellationToken
    )
    {
        ArgumentNullException.ThrowIfNull(user);

        logger.LogInformation(
            "Mail to {Email}: reset the password with POST /auth/reset-password, token {Token}",
            user.Email,
            encodedToken
        );
        return Task.CompletedTask;
    }
}
