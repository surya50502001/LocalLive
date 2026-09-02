using LocalLive.Application.Common;
using LocalLive.Application.Features.Requests;

namespace LocalLive.Application.Common.Interfaces;

public interface IRequestService
{
    Task<Result<RequestDto>> CreateAsync(Guid customerUserId, CreateRequestRequest request);
    Task<Result<RequestDto>> GetByIdAsync(Guid requestId, Guid? viewerUserId, string viewerRole);
    Task<Result<RequestDto>> CancelAsync(Guid customerUserId, Guid requestId);
    Task<Result<RequestDto>> FulfillAsync(Guid customerUserId, Guid requestId);
    Task<Result<RequestDto>> AvailableAsync(Guid shopOwnerUserId, Guid requestId, string? message);
    Task<List<RequestDto>> GetMyLiveRequestsAsync(Guid customerUserId);
    Task<List<RequestDto>> GetShopLiveRequestsAsync(Guid shopOwnerUserId);
    Task MarkExpiredRequestsAsync();
}
