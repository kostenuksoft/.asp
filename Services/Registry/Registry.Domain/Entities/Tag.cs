using Registry.Domain.Exceptions;

namespace Registry.Domain.Entities;

public sealed class Tag : AuditableEntity
{
    private readonly List<PackageTag> _packageTags = [];

    private Tag()
    {
    }

    public Tag(
        string name,
        string? description
    )
    {
        if (string.IsNullOrWhiteSpace(name) || name.Length < 2 || !name.All(IsNameCharacter))
        {
            throw new DomainException("Tag name must have at least 2 characters: lowercase letters, digits or hyphens.");
        }

        Name = name;
        Description = description;
    }

    public long TagId { get; private set; }

    public string Name { get; private set; } = string.Empty;

    public string? Description { get; private set; }

    public IReadOnlyCollection<PackageTag> PackageTags => _packageTags;

    public void Describe(string? description) => Description = description;

    private static bool IsNameCharacter(
        char character
    ) => char.IsAsciiLetterLower(character) || char.IsAsciiDigit(character) || character == '-';
}
