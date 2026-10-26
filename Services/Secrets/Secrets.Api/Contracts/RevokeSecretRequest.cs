using System.ComponentModel.DataAnnotations;

namespace Secrets.Api.Contracts;

public sealed record RevokeSecretRequest([Required, StringLength(100)] string Name);
