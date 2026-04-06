namespace Backend.Models;

public sealed record LiveRouteSnapshot(
    DateTimeOffset LastUpdatedUtc,
    IReadOnlyList<RouteVehiclePosition> Vehicles);
