namespace Registry.Domain.Entities;

public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; private set; }

    public string CreatedBy { get; private set; } = null!;

    public DateTime? UpdatedAt { get; private set; }

    public string? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public void MarkCreated(string createdBy) => CreatedBy = createdBy;

    public void MarkUpdated(
        DateTime updatedAt,
        string? updatedBy
    )
    {
        UpdatedAt = updatedAt;
        UpdatedBy = updatedBy;
    }

    public void MarkDeleted(
        DateTime deletedAt,
        string? deletedBy
    )
    {
        IsDeleted = true;
        UpdatedAt = deletedAt;
        UpdatedBy = deletedBy;
    }
}
