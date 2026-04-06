using Backend.Hubs;
using Backend.Services;
using Microsoft.AspNetCore.SignalR;

namespace Backend.BackgroundWorkers;

public sealed class RoutesPollingWorker : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(30);

    private readonly ILogger<RoutesPollingWorker> _logger;
    private readonly IRouteLiveService _routeLiveService;
    private readonly IHubContext<RoutesHub> _hubContext;

    public RoutesPollingWorker(
        ILogger<RoutesPollingWorker> logger,
        IRouteLiveService routeLiveService,
        IHubContext<RoutesHub> hubContext)
    {
        _logger = logger;
        _routeLiveService = routeLiveService;
        _hubContext = hubContext;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var snapshot = await _routeLiveService.RefreshAsync(stoppingToken);

            await _hubContext.Clients.All.SendAsync("routesUpdated", snapshot, stoppingToken);

            _logger.LogInformation("Published live route snapshot at {Timestamp}", snapshot.LastUpdatedUtc);

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }
}
