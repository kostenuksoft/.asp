namespace Registry.Domain.Entities;

public abstract class AuditableEntity
{
    public DateTime CreatedAt { get; private set; }

    public string CreatedBy { get; private set; } = null!;

    public DateTime? UpdatedAt { get; private set; }

    public string? UpdatedBy { get; private set; }

    public bool IsDeleted { get; private set; }

    public void MarkUpdated(DateTime updatedAt) => UpdatedAt = updatedAt;

    public void MarkDeleted(DateTime deletedAt)
    {
        IsDeleted = true;
        UpdatedAt = deletedAt;
    }
}
