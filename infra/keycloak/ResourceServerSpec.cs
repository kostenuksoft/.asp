namespace KeycloakRealm;

public sealed record ResourceServerSpec(
    string ClientId,
    string DisplayName,
    IReadOnlyList<string> Scopes,
    IReadOnlyDictionary<string, IReadOnlyList<string>> ScopesByRole
);
