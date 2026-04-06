namespace Backend.Models;

public sealed record AynaBusDirection(
    int DirectionTypeId,
    IReadOnlyList<GeoPointDto>? FlowCoordinates);
