namespace Backend.Models;

public sealed record AynaBusStop(
    int DirectionTypeId,
    int StopId,
    string? StopName);
