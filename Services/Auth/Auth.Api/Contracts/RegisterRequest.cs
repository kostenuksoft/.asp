using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public sealed record RegisterRequest(
    [Required, StringLength(64, MinimumLength = 3), RegularExpression("^[a-zA-Z0-9._-]+$")] string UserName,
    [Required, EmailAddress, StringLength(256)] string Email,
    [Required, StringLength(128, MinimumLength = 8)] string Password,
    [StringLength(100)] string? DisplayName
);
