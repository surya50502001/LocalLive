using LocalLive.Domain.Common;
using LocalLive.Domain.Enums;

namespace LocalLive.Domain.Entities;

public class ShopVerification : BaseEntity
{
    public Guid ShopId { get; set; }
    public Shop? Shop { get; set; }

    public string DocumentType { get; set; } = string.Empty;
    public string DocumentUrl { get; set; } = string.Empty;
    public string? BusinessRegistrationNumber { get; set; }

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ReviewedAt { get; set; }
    public Guid? ReviewedByAdminUserId { get; set; }
    public User? ReviewedByAdminUser { get; set; }

    public ShopStatus Status { get; set; } = ShopStatus.Pending;
    public string? AdminNotes { get; set; }
}
