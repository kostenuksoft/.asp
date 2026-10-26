using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public sealed record AssignRoleRequest([Required] string Role);
