namespace Backend.Models;

public sealed record LiveRouteDto(
    int RouteId,
    string RouteNumber,
    string Carrier,
    string FirstPoint,
    string LastPoint,
    double RouteLengthKm,
    string? Tariff,
    int DurationMinutes,
    string? RegionName,
    string? WorkingZoneType,
    IReadOnlyList<LiveRouteDirectionDto> Directions);
