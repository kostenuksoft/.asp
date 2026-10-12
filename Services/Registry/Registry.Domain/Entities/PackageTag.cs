using Registry.Domain.Exceptions;

namespace Registry.Domain.Entities;

public sealed class PackageTag
{
    private PackageTag()
    {
    }

    public PackageTag(
        long packageId,
        long tagId,
        DateTime assignedAt
    )
    {
        if (packageId <= 0 || tagId <= 0)
        {
            throw new DomainException("A tag assignment needs an existing package and an existing tag.");
        }

        PackageId = packageId;
        TagId = tagId;
        AssignedAt = assignedAt;
    }

    internal PackageTag(
        Package package,
        long tagId,
        DateTime assignedAt
    )
    {
        if (tagId <= 0)
        {
            throw new DomainException("A tag assignment needs an existing tag.");
        }

        Package = package;
        TagId = tagId;
        AssignedAt = assignedAt;
    }

    public long PackageId { get; private set; }

    public Package Package { get; private set; } = null!;

    public long TagId { get; private set; }

    public Tag Tag { get; private set; } = null!;

    public DateTime AssignedAt { get; private set; }

    public DateTime CreatedAt { get; private set; }

    public string CreatedBy { get; private set; } = null!;
}
