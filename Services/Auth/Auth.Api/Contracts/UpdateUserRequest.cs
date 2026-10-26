using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public sealed record UpdateUserRequest([StringLength(100)] string? DisplayName);
