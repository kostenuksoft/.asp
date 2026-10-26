using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public sealed record VerifyEmailRequest(
    [Required] string UserId,
    [Required] string Token
);
