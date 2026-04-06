using Backend.Data;
using Backend.Models;

namespace Backend.Services;

public sealed class RouteLiveService : IRouteLiveService
{
    private readonly InMemoryRouteStore _routeStore;

    public RouteLiveService(InMemoryRouteStore routeStore)
    {
        _routeStore = routeStore;
    }

    public Task<LiveRouteSnapshot> GetCurrentSnapshotAsync(CancellationToken cancellationToken)
    {
        return Task.FromResult(_routeStore.GetSnapshot());
    }

    public Task<LiveRouteSnapshot> RefreshAsync(CancellationToken cancellationToken)
    {
        var timestamp = DateTimeOffset.UtcNow;
        var drift = (timestamp.Second % 6) * 0.0004;

        var snapshot = new LiveRouteSnapshot(
            LastUpdatedUtc: timestamp,
            Vehicles:
            [
                new RouteVehiclePosition("BUS-101", "R-01", 40.4093 + drift, 49.8671 + drift, "In Service"),
                new RouteVehiclePosition("BUS-205", "R-05", 40.3957 - drift, 49.8822 + drift, "Approaching Stop"),
                new RouteVehiclePosition("BUS-318", "R-08", 40.4188 + drift, 49.8387 - drift, "Delayed")
            ]);

        _routeStore.SetSnapshot(snapshot);

        return Task.FromResult(snapshot);
    }
}
