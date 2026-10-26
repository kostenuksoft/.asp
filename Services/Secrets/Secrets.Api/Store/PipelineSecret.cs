namespace Secrets.Api.Store;

public sealed class PipelineSecret(
    string name,
    string fingerprint,
    DateTime createdAt
)
{
    public string Name { get; } = name;

    public int Version { get; private set; } = 1;

    public string Fingerprint { get; private set; } = fingerprint;

    public DateTime CreatedAt { get; } = createdAt;

    public DateTime? RotatedAt { get; private set; }

    public DateTime? RevokedAt { get; private set; }

    public bool IsRevoked => RevokedAt is not null;

    public void Rotate(
        string fingerprint,
        DateTime rotatedAt
    )
    {
        Fingerprint = fingerprint;
        Version++;
        RotatedAt = rotatedAt;
        RevokedAt = null;
    }

    public void Revoke(DateTime revokedAt) => RevokedAt ??= revokedAt;
}
