using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Secrets.Api.Store;

public sealed class PipelineSecretStore(TimeProvider timeProvider)
{
    private readonly ConcurrentDictionary<string, PipelineSecret> _secrets = new(
        new Dictionary<string, PipelineSecret>
        {
            ["registry-publish-token"] = new("registry-publish-token", Fingerprint("registry-publish-token-v1"), new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc)),
            ["package-signing-certificate"] = new("package-signing-certificate", Fingerprint("package-signing-certificate-v1"), new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc)),
            ["deploy-ssh-key"] = new("deploy-ssh-key", Fingerprint("deploy-ssh-key-v1"), new DateTime(2026, 10, 1, 9, 0, 0, DateTimeKind.Utc)),
        },
        StringComparer.OrdinalIgnoreCase
    );

    public IReadOnlyList<PipelineSecret> GetAll() => _secrets.Values
        .OrderBy(secret => secret.Name, StringComparer.Ordinal)
        .ToList();

    public PipelineSecret? Find(string name) => _secrets.TryGetValue(name, out PipelineSecret? secret) ? secret : null;

    public PipelineSecret? Rotate(
        string name,
        string newValue
    )
    {
        PipelineSecret? secret = Find(name);
        lock (_secrets)
        {
            secret?.Rotate(Fingerprint(newValue), timeProvider.GetUtcNow().UtcDateTime);
        }

        return secret;
    }

    public PipelineSecret? Revoke(string name)
    {
        PipelineSecret? secret = Find(name);
        lock (_secrets)
        {
            secret?.Revoke(timeProvider.GetUtcNow().UtcDateTime);
        }

        return secret;
    }

    private static string Fingerprint(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)))[..16];
}
