using Backend.Models;

namespace Backend.Data;

public sealed class InMemoryRouteStore
{
    private readonly object _syncRoot = new();

    private LiveRouteSnapshot _snapshot = new(
        LastUpdatedUtc: DateTimeOffset.UtcNow,
        Vehicles:
        [
            new RouteVehiclePosition("BUS-101", "R-01", 40.4093, 49.8671, "In Service"),
            new RouteVehiclePosition("BUS-205", "R-05", 40.3957, 49.8822, "Approaching Stop"),
            new RouteVehiclePosition("BUS-318", "R-08", 40.4188, 49.8387, "Delayed")
        ]);

    public LiveRouteSnapshot GetSnapshot()
    {
        lock (_syncRoot)
        {
            return _snapshot;
        }
    }

    public void SetSnapshot(LiveRouteSnapshot snapshot)
    {
        lock (_syncRoot)
        {
            _snapshot = snapshot;
        }
    }
}
