using LocalLive.Domain.Common;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;

namespace LocalLive.Domain.Entities;

public class Report : BaseEntity
{
    public Guid ReporterUserId { get; set; }
    public User? ReporterUser { get; set; }

    public ReportTargetType TargetType { get; set; }
    public Guid TargetId { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string? Details { get; set; }

    public ReportStatus Status { get; set; } = ReportStatus.Open;
    public Guid? ResolvedByUserId { get; set; }
    public User? ResolvedByUser { get; set; }
    public DateTime? ResolvedAt { get; set; }
    public string? ResolutionNote { get; set; }
}
