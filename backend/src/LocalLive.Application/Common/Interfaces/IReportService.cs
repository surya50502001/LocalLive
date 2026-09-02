using LocalLive.Application.Common;
using LocalLive.Application.Features.Reports;

namespace LocalLive.Application.Common.Interfaces;

public interface IReportService
{
    Task<Result<ReportDto>> CreateAsync(Guid reporterUserId, CreateReportRequest request);
}
