namespace Backend.Models;

public sealed record LiveRouteSnapshot(
    DateTimeOffset LastUpdatedUtc,
    string Provider,
    int TotalRoutes,
    IReadOnlyList<LiveRouteDto> Routes);
