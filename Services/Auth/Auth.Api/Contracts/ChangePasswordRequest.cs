using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public sealed record ChangePasswordRequest(
    [Required] string CurrentPassword,
    [Required, StringLength(128, MinimumLength = 8)] string NewPassword
);
