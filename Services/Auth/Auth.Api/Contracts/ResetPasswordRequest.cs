using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public sealed record ResetPasswordRequest(
    [Required, EmailAddress] string Email,
    [Required] string Token,
    [Required, StringLength(128, MinimumLength = 8)] string NewPassword
);
