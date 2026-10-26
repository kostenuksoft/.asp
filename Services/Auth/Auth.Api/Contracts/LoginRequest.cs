using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public sealed record LoginRequest(
    [Required] string UserName,
    [Required] string Password
);
