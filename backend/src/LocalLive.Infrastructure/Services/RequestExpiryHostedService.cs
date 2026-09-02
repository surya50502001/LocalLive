using LocalLive.Application.Common.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace LocalLive.Infrastructure.Services;

/// <summary>
/// Periodically marks active requests as expired once their ExpiresAt passes.
/// Fires the realtime status-changed events so clients auto-remove the request.
/// </summary>
public class RequestExpiryHostedService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly ILogger<RequestExpiryHostedService> _logger;
    private static readonly TimeSpan Interval = TimeSpan.FromSeconds(20);

    public RequestExpiryHostedService(IServiceScopeFactory scopeFactory, ILogger<RequestExpiryHostedService> logger)
    {
        _scopeFactory = scopeFactory;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await timer.WaitForNextTickAsync(stoppingToken);
                using var scope = _scopeFactory.CreateScope();
                var requestService = scope.ServiceProvider.GetRequiredService<IRequestService>();
                await requestService.MarkExpiredRequestsAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error while expiring requests.");
            }
        }
    }
}
