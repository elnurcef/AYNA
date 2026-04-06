using Backend.Hubs;
using Backend.Models;
using Backend.Services;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Options;

namespace Backend.BackgroundWorkers;

public sealed class RoutesPollingWorker : BackgroundService
{
    private readonly ILogger<RoutesPollingWorker> _logger;
    private readonly IRouteLiveService _routeLiveService;
    private readonly IHubContext<RoutesHub> _hubContext;
    private readonly LiveRoutesOptions _options;

    public RoutesPollingWorker(
        ILogger<RoutesPollingWorker> logger,
        IRouteLiveService routeLiveService,
        IHubContext<RoutesHub> hubContext,
        IOptions<LiveRoutesOptions> options)
    {
        _logger = logger;
        _routeLiveService = routeLiveService;
        _hubContext = hubContext;
        _options = options.Value;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        var pollingInterval = TimeSpan.FromSeconds(Math.Max(_options.RefreshIntervalSeconds, 5));

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                var snapshot = await _routeLiveService.RefreshAsync(stoppingToken);

                await _hubContext.Clients.All.SendAsync(
                    RoutesHub.RoutesUpdatedEvent,
                    snapshot,
                    stoppingToken);

                _logger.LogInformation(
                    "Published live route snapshot from {Provider} at {Timestamp}",
                    snapshot.Provider,
                    snapshot.LastUpdatedUtc);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogWarning(
                    exception,
                    "Failed to refresh live route snapshot. Keeping the previous successful result.");
            }

            await Task.Delay(pollingInterval, stoppingToken);
        }
    }
}
