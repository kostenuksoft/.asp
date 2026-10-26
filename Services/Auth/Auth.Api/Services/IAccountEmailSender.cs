using Auth.Api.Data;

namespace Auth.Api.Services;

public interface IAccountEmailSender
{
    Task SendEmailConfirmationAsync(
        ApplicationUser user,
        string encodedToken,
        CancellationToken cancellationToken
    );

    Task SendPasswordResetAsync(
        ApplicationUser user,
        string encodedToken,
        CancellationToken cancellationToken
    );
}
