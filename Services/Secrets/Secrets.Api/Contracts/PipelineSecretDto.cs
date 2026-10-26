using Secrets.Api.Store;

namespace Secrets.Api.Contracts;

public sealed record PipelineSecretDto(
    string Name,
    int Version,
    string Fingerprint,
    DateTime CreatedAt,
    DateTime? RotatedAt,
    DateTime? RevokedAt
)
{
    public static PipelineSecretDto From(PipelineSecret secret)
    {
        ArgumentNullException.ThrowIfNull(secret);

        return new PipelineSecretDto(
            secret.Name,
            secret.Version,
            secret.Fingerprint,
            secret.CreatedAt,
            secret.RotatedAt,
            secret.RevokedAt
        );
    }
}
