namespace Backend.Models;

public sealed record RouteVehiclePosition(
    string VehicleId,
    string RouteId,
    double Latitude,
    double Longitude,
    string Status);
