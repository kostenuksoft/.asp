using System.ComponentModel.DataAnnotations;

namespace Auth.Api.Contracts;

public sealed record EmailRequest([Required, EmailAddress] string Email);
