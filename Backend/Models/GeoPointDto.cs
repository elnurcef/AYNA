using System.Text.Json.Serialization;

namespace Backend.Models;

public sealed record GeoPointDto
{
    public GeoPointDto(double latitude, double longitude)
    {
        Latitude = latitude;
        Longitude = longitude;
    }

    [JsonPropertyName("lat")]
    public double Latitude { get; init; }

    [JsonPropertyName("lng")]
    public double Longitude { get; init; }
}
