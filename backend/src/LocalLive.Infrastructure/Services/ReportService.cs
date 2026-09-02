using LocalLive.Application.Common;
using LocalLive.Application.Common.Interfaces;
using LocalLive.Application.Features.Reports;
using LocalLive.Domain.Entities;
using LocalLive.Domain.Enums;
using LocalLive.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace LocalLive.Infrastructure.Services;

public class ReportService : IReportService
{
    private readonly AppDbContext _db;

    public ReportService(AppDbContext db)
    {
        _db = db;
    }

    public async Task<Result<ReportDto>> CreateAsync(Guid reporterUserId, CreateReportRequest request)
    {
        if (request.TargetType == ReportTargetType.Shop)
        {
            var exists = await _db.Shops.AnyAsync(s => s.Id == request.TargetId);
            if (!exists)
            {
                return Result<ReportDto>.Failure(new Error(ErrorType.NotFound, "SHOP_NOT_FOUND", "Reported shop not found."));
            }
        }
        else if (request.TargetType == ReportTargetType.Request)
        {
            var exists = await _db.LiveRequests.AnyAsync(r => r.Id == request.TargetId);
            if (!exists)
            {
                return Result<ReportDto>.Failure(new Error(ErrorType.NotFound, "REQUEST_NOT_FOUND", "Reported request not found."));
            }
        }

        var report = new Report
        {
            ReporterUserId = reporterUserId,
            TargetType = request.TargetType,
            TargetId = request.TargetId,
            Reason = request.Reason.Trim(),
            Status = ReportStatus.Open
        };
        _db.Reports.Add(report);
        await _db.SaveChangesAsync();

        return Result<ReportDto>.Success(Map(report));
    }

    private static ReportDto Map(Report r) => new()
    {
        Id = r.Id,
        ReporterUserId = r.ReporterUserId,
        TargetType = r.TargetType.ToString(),
        TargetId = r.TargetId,
        Reason = r.Reason,
        Status = r.Status.ToString(),
        CreatedAt = r.CreatedAt
    };
}
