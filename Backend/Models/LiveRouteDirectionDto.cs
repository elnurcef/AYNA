namespace Backend.Models;

public sealed record LiveRouteDirectionDto(
    int DirectionTypeId,
    string DirectionName,
    int StopCount,
    IReadOnlyList<GeoPointDto> Path);
