using Backend.Models;

namespace Backend.Helpers;

public static class AynaLiveRouteMapper
{
    public static LiveRouteSnapshot MapSnapshot(IEnumerable<AynaBusDetail> details, string providerName)
    {
        var routes = details
            .Where(detail => !string.IsNullOrWhiteSpace(detail.Number))
            .Select(MapRoute)
            .OrderBy(route => route.RouteNumber, RouteCodeComparer.Instance)
            .ToArray();

        return new LiveRouteSnapshot(
            LastUpdatedUtc: DateTimeOffset.UtcNow,
            Provider: providerName,
            TotalRoutes: routes.Length,
            Routes: routes);
    }

    private static LiveRouteDto MapRoute(AynaBusDetail detail)
    {
        var directions = (detail.Routes ?? [])
            .Select(direction => MapDirection(direction, detail.Stops ?? []))
            .Where(direction => direction.Path.Count > 0)
            .OrderBy(direction => direction.DirectionTypeId)
            .ToArray();

        return new LiveRouteDto(
            RouteId: detail.Id,
            RouteNumber: detail.Number,
            Carrier: detail.Carrier ?? string.Empty,
            FirstPoint: detail.FirstPoint ?? string.Empty,
            LastPoint: detail.LastPoint ?? string.Empty,
            RouteLengthKm: detail.RoutLength,
            Tariff: detail.TariffStr,
            DurationMinutes: detail.DurationMinuts,
            RegionName: detail.Region?.Name,
            WorkingZoneType: detail.WorkingZoneType?.Name,
            Directions: directions);
    }

    private static LiveRouteDirectionDto MapDirection(
        AynaBusDirection direction,
        IReadOnlyList<AynaBusStop> stops)
    {
        var path = (direction.FlowCoordinates ?? [])
            .Where(point => double.IsFinite(point.Latitude) && double.IsFinite(point.Longitude))
            .ToArray();

        var stopCount = stops.Count(stop => stop.DirectionTypeId == direction.DirectionTypeId);

        return new LiveRouteDirectionDto(
            DirectionTypeId: direction.DirectionTypeId,
            DirectionName: GetDirectionName(direction.DirectionTypeId),
            StopCount: stopCount,
            Path: path);
    }

    private static string GetDirectionName(int directionTypeId)
    {
        return directionTypeId switch
        {
            1 => "forward",
            2 => "backward",
            _ => $"direction-{directionTypeId}"
        };
    }
}
