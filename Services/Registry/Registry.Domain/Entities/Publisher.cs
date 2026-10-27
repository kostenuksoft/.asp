using Registry.Domain.Exceptions;

namespace Registry.Domain.Entities;

public sealed class Publisher : AuditableEntity
{
    private readonly List<Package> _packages = [];

    private Publisher()
    {
    }

    public Publisher(
        string slug,
        string displayName,
        string contactEmail
    )
    {
        if (string.IsNullOrWhiteSpace(slug) || slug.Length < 2 || !slug.All(IsSlugCharacter))
        {
            throw new DomainException(
                "Publisher slug must have at least 2 characters: lowercase letters, digits or hyphens."
            );
        }

        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new DomainException("Publisher display name is required.");
        }

        Slug = slug;
        DisplayName = displayName;
        ChangeContactEmail(contactEmail);
    }

    public long PublisherId { get; private set; }

    public string Slug { get; private set; } = string.Empty;

    public string DisplayName { get; private set; } = string.Empty;

    public string ContactEmail { get; private set; } = string.Empty;

    public bool IsVerified { get; private set; }

    public byte[] RowVersion { get; private set; } = [];

    public IReadOnlyCollection<Package> Packages => _packages;

    public void ChangeContactEmail(string contactEmail)
    {
        if (string.IsNullOrWhiteSpace(contactEmail) || !contactEmail.Contains('@', StringComparison.Ordinal))
        {
            throw new DomainException("Publisher contact email is not a valid address.");
        }

        ContactEmail = contactEmail;
    }

    public void Verify()
    {
        if (IsVerified)
        {
            throw new DomainException($"Publisher '{Slug}' is already verified.");
        }

        IsVerified = true;
    }

    private static bool IsSlugCharacter(
        char character
    ) => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-';
}
