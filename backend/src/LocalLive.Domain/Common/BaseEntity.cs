namespace LocalLive.Domain.Common;

public abstract class BaseEntity
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? DeletedAt { get; set; }

    public bool IsDeleted => DeletedAt.HasValue;
    public void MarkUpdated() => UpdatedAt = DateTime.UtcNow;
    public void SoftDelete() => DeletedAt = DateTime.UtcNow;
}
