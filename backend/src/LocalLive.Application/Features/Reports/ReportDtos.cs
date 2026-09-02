using System.ComponentModel.DataAnnotations;
using LocalLive.Domain.Enums;

namespace LocalLive.Application.Features.Reports;

public record CreateReportRequest
{
    [Required]
    public ReportTargetType TargetType { get; init; }

    [Required]
    public Guid TargetId { get; init; }

    [Required, MinLength(5), MaxLength(1000)]
    public string Reason { get; init; } = string.Empty;
}

public record ReportDto
{
    public Guid Id { get; init; }
    public Guid ReporterUserId { get; init; }
    public string TargetType { get; init; } = string.Empty;
    public Guid TargetId { get; init; }
    public string Reason { get; init; } = string.Empty;
    public string Status { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
