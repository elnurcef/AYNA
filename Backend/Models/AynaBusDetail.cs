namespace Backend.Models;

public sealed record AynaBusDetail(
    int Id,
    string Number,
    string? Carrier,
    string? FirstPoint,
    string? LastPoint,
    double RoutLength,
    string? TariffStr,
    int DurationMinuts,
    AynaNamedEntity? Region,
    AynaNamedEntity? WorkingZoneType,
    IReadOnlyList<AynaBusStop>? Stops,
    IReadOnlyList<AynaBusDirection>? Routes);
