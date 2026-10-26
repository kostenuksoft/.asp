using System.ComponentModel.DataAnnotations;

namespace Secrets.Api.Contracts;

public sealed record RotateSecretRequest(
    [Required, StringLength(100)] string Name,
    [Required, StringLength(4096, MinimumLength = 16)] string NewValue
);
